using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace SolidModelBrowser
{
    public partial class MainWindow : Window
    {
        const string version = "0.3";
        Point3D modelCenter = new Point3D();
        Vector3D modelSize = new Vector3D();

        string lastFilename;

        UCamera camera;

        DispatcherTimer loadingTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100), IsEnabled = false };

        List<Import> imports = new List<Import>() { new ImportSTL(), new Import3MF(), new ImportOBJ() };
        Import currentImport = null;

        Settings settings = new Settings();

        public MainWindow()
        {
            InitializeComponent();
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; // to avoid "." "," mess while parsing
                        
            filePanel.IsIgnoringLetterKeysNavigation = true;
            filePanel.Extensions = new List<string>();
            foreach (Import importer in imports) filePanel.Extensions.AddRange(importer.Extensions);
            filePanel.SelectionChanged += (s, e) => clearView();
            filePanel.FileNavigated += (s, filename) => loadFile(filename);

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            
            camera = new UCamera(viewport3D);

            tempForAxes = axesgroup;
            modelgroup.Children.Remove(axesgroup);
            tempForGround = ground;
            modelgroup.Children.Remove(ground);

            showInfo($"Solid Model Browser v {version}\r\nSTL, OBJ, 3MF formats supported\r\n\r\nquestfulcat 2024 (C)\r\nhttps://github.com/questfulcat/solidmodelbrowser\r\nDistributed under MIT License\r\n\r\nF1 - keymap help");

            loadingTimer.Tick += LoadingTimer_Tick;

            loadSettings();
            this.Closing += (s, e) => saveSettings();

            // set max window size to avoid taskbar overlay
            var scrsize = Utils.GetCurrentScreenSize(this);
            MaxWidth = settings.MaxWidth < 1 ? scrsize.Width : Math.Max(settings.MaxWidth, MinWidth);
            MaxHeight = settings.MaxHeight < 1 ? scrsize.Height : Math.Max(settings.MaxHeight, MinHeight);

            var args = Environment.GetCommandLineArgs();
            if (args.Length == 2 && File.Exists(args[1]) && selectImporter(args[1])) loadFile(args[1]);

        }

        void saveSettings()
        {
            if (this.WindowState == WindowState.Normal)
            {
                settings.LocationX = this.Left;
                settings.LocationY = this.Top;
                settings.Width = this.Width;
                settings.Height = this.Height;
            }
            settings.WindowState = this.WindowState;
            settings.FilePanelWidth = FilePanelColumn.Width.Value;
            settings.SelectedPath = filePanel.Path;

            settings.IsDiffuseEnabled = ButtonDiffuseMaterial.IsChecked.Value;
            settings.IsSpecularEnabled = ButtonSpecularMaterial.IsChecked.Value;
            settings.IsEmissiveEnabled = ButtonEmisiveMaterial.IsChecked.Value;
            settings.IsBackDiffuseEnabled = ButtonBacksideDiffuseMaterial.IsChecked.Value;
            settings.IsOrthoCameraEnabled = ButtonSelectCamera.IsChecked.Value;
            settings.IsFishEyeModeEnabled = ButtonFishEyeFOV.IsChecked.Value;
            settings.IsAxesEnabled = ButtonAxes.IsChecked.Value;
            settings.IsGroundEnabled = ButtonGround.IsChecked.Value;

            settings.Save();
        }

        void showInfo(string info)
        {
            textBlockInfo.Text = info;
            infoContainer.Visibility = Visibility.Visible;
        }

        void hideInfo() => infoContainer.Visibility = Visibility.Collapsed;

        void loadSettings()
        {
            settings.Load();
            if (settings.GetParseErrorsCount() > 0) MessageBox.Show(String.Join("\r\n", settings.GetParseErrors().ToArray()));
            setAppTheme();
            this.Left = settings.LocationX;
            this.Top = settings.LocationY;
            this.Width = settings.Width;
            this.Height = settings.Height;
            this.WindowState = settings.WindowState;
            FilePanelColumn.Width = new GridLength(settings.FilePanelWidth);
            filePanel.Path = settings.SelectedPath;

            ButtonDiffuseMaterial.IsChecked = settings.IsDiffuseEnabled;
            ButtonSpecularMaterial.IsChecked = settings.IsSpecularEnabled;
            ButtonEmisiveMaterial.IsChecked = settings.IsEmissiveEnabled;
            ButtonBacksideDiffuseMaterial.IsChecked = settings.IsBackDiffuseEnabled;
            ButtonSelectCamera.IsChecked = settings.IsOrthoCameraEnabled;
            ButtonFishEyeFOV.IsChecked = settings.IsFishEyeModeEnabled;
            ButtonAxes.IsChecked = settings.IsAxesEnabled;
            ButtonGround.IsChecked = settings.IsGroundEnabled;

            settings.FOV = Utils.MinMax(settings.FOV, 5.0, 160.0);
            settings.FishEyeFOV = Utils.MinMax(settings.FishEyeFOV, 5.0, 160.0);
            settings.CameraInitialShift = Utils.MinMax(settings.CameraInitialShift, 0.5, 10.0);
            settings.ModelRotationAngle = Utils.SelectInRange(settings.ModelRotationAngle, new double[] {45.0, 90.0}, 90.0);

            updateCameraModes();
            updateAxes();
            updateGround();

            createMaterials();
            applyMaterials();
        }

        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                //textBlockProgress.Text = ImportBase.Progress.ToString();
                borderProgressContainer.Visibility = Visibility.Visible;
                borderProgress.Width = Import.Progress < 0 ? 0 : (Import.Progress > 100 ? 100 : Import.Progress);

                if (currentTask?.Status == TaskStatus.Running) return;
                loadingTimer.Stop();
                borderProgressContainer.Visibility = Visibility.Hidden;

                if (Import.ExceptionMessage != null)
                {
                    clearView();
                    showInfo(Import.ExceptionMessage);
                    return;
                }

                Utils.FillMesh(mesh);
                Utils.GetModelCenterAndSize(mesh, out modelCenter, out modelSize);
                camera.LookCenter = modelCenter;
                camera.DefaultPosition(modelSize.Length, settings.CameraInitialShift);
                //Utils.RecreateUnsmoothed(mesh);

                if (currentImport.InitialXRotationNeeded) addRotation(Axis.X, 90.0);

                if (ButtonShowModelInfo.IsChecked.Value) showModelInfo();
                else hideInfo();
            }
            catch (Exception exc)
            {
                loadingTimer.Stop();
                clearView();
                showInfo(exc.Message);
            }
        }

        bool selectImporter(string filename)
        {
            string ext = Path.GetExtension(filename).Trim('.').ToLower();
            foreach(Import i in imports) 
                if (i.Extensions.Contains(ext)) { currentImport = i; return true; }
            return false;
        }

        Task currentTask = null;
        void loadFile(string filename)
        {
            lastFilename = null;
            try
            {
                if (currentTask?.Status == TaskStatus.Running)
                {
                    Import.StopFlag = true;
                    currentTask.Wait(3000);
                }
                currentTask?.Dispose();

                if (!selectImporter(filename)) { clearView(); return; }
                currentImport.Initialize();

                currentTask = new Task(() =>
                {
                    try
                    {
                        currentImport.Load(filename);
                    }
                    catch (Exception exc) { Import.ExceptionMessage = exc.Message; }
                });

                lastFilename = filename;
                loadingTimer.Start();
                showInfo("loading...");

                currentTask.Start();
                
                //showInfo($"{lookCenter.X}, {lookCenter.Y}, {lookCenter.Z}");
            }
            catch (Exception exc)
            {
                showInfo(exc.Message);
            }
        }

        enum Axis { X = 1, Y = 2, Z = 4 }
        void addRotation(Axis axis, double angle) => transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(axis == Axis.X ? 1 : 0, axis == Axis.Y ? 1 : 0, axis == Axis.Z ? 1 : 0), angle), modelCenter));

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string CAS = Utils.GetKeyboardModifiers();

            if (CAS == "") // no modifier keys
            {
                double ra = settings.ModelRotationAngle;
                if (e.Key == Key.A) addRotation(Axis.Z, ra);
                if (e.Key == Key.D) addRotation(Axis.Z, -ra);
                if (e.Key == Key.S) addRotation(Axis.X, ra);
                if (e.Key == Key.W) addRotation(Axis.X, -ra);
                if (e.Key == Key.E) addRotation(Axis.Y, ra);
                if (e.Key == Key.Q) addRotation(Axis.Y, -ra);

                double cis = settings.CameraInitialShift;
                if (e.Key == Key.G) camera.RelativePosition(modelSize.Length, 0, -cis, 0);
                if (e.Key == Key.R) camera.RelativePosition(modelSize.Length, 0, cis, 0);
                if (e.Key == Key.F) camera.RelativePosition(modelSize.Length, -cis, 0, 0);
                if (e.Key == Key.H) camera.RelativePosition(modelSize.Length, cis, 0, 0);
                if (e.Key == Key.T) camera.RelativePosition(modelSize.Length, 0, -0.0001, cis);
                if (e.Key == Key.B) camera.RelativePosition(modelSize.Length, 0, -0.0001, -cis);

                if (e.Key == Key.F1)
                {
                    ButtonShowModelInfo.IsChecked = false;
                    if (textBlockInfo.Text == help && infoContainer.Visibility == Visibility.Visible) hideInfo();
                    else showInfo(help);
                }
                if (e.Key == Key.F2)
                {
                    ButtonDiffuseMaterial.IsChecked = !ButtonDiffuseMaterial.IsChecked;
                    applyMaterials();
                }
                if (e.Key == Key.F3)
                {
                    ButtonSpecularMaterial.IsChecked = !ButtonSpecularMaterial.IsChecked;
                    applyMaterials();
                }
                if (e.Key == Key.F4)
                {
                    ButtonEmisiveMaterial.IsChecked = !ButtonEmisiveMaterial.IsChecked;
                    applyMaterials();
                }
                if (e.Key == Key.F5)
                {
                    clearView();
                    if (lastFilename != null) loadFile(lastFilename);
                }
                if (e.Key == Key.F6)
                {
                    ButtonSaveImage_Click(sender, e);
                }
                if (e.Key == Key.F7)
                {
                    ButtonOpenExtApp_Click(sender, e);
                }
                if (e.Key == Key.F8)
                {
                    ButtonSelectCamera.IsChecked = !ButtonSelectCamera.IsChecked;
                    camera.SelectType(ButtonSelectCamera.IsChecked.Value);
                }
                if (e.Key == Key.C)
                {
                    camera.TurnAt(modelCenter);
                }

                if (e.Key == Key.O)
                {
                    ButtonAxes.IsChecked = !ButtonAxes.IsChecked;
                    updateAxes();
                }
                if (e.Key == Key.P)
                {
                    ButtonGround.IsChecked = !ButtonGround.IsChecked;
                    updateGround();
                }

                if (e.Key == Key.I)
                {
                    ButtonShowModelInfo.IsChecked = !ButtonShowModelInfo.IsChecked;
                    updateModelInfo();
                }
            }

            if (CAS == "C") // CTRL
            {
                if (e.Key == Key.F) Utils.RecreateUnsmoothed(mesh);
            }
        }
        
        void clearView()
        {
            mesh.TriangleIndices.Clear();
            mesh.Positions.Clear();
            mesh.Normals.Clear();
            transform.Children.Clear();
            hideInfo();
        }

        void showModelInfo()
        {
            showInfo($"File: {lastFilename}\r\n\r\nVertices: {mesh.Positions.Count}\r\nTriangles: {mesh.TriangleIndices.Count / 3}\r\nNormals: {mesh.Normals.Count}\r\n\r\nSize X:{modelSize.X.ToString("0.00")}, Y:{modelSize.Y.ToString("0.00")}, Z:{modelSize.Z.ToString("0.00")}\r\nCenter X:{modelCenter.X.ToString("0.00")}, Y:{modelCenter.Y.ToString("0.00")}, Z:{modelCenter.Z.ToString("0.00")}");
        }


        DiffuseMaterial matDiffuse;
        SpecularMaterial matSpecular;
        EmissiveMaterial matEmissive;
        DiffuseMaterial matBackDiffuse;
        void createMaterials()
        {
            matDiffuse = new DiffuseMaterial(new SolidColorBrush(settings.DiffuseColor.Color));
            //matDiffuse.AmbientColor = Color.FromArgb(255, 0, 255, 0);
            matSpecular = new SpecularMaterial(new SolidColorBrush(settings.SpecularColor.Color), settings.SpecularPower);
            matEmissive = new EmissiveMaterial(new SolidColorBrush(settings.EmissiveColor.Color));
            matBackDiffuse = new DiffuseMaterial(new SolidColorBrush(settings.BackDiffuseColor.Color));
        }

        void applyMaterials()
        {
            materialGroup.Children.Clear();
            if (ButtonDiffuseMaterial.IsChecked.Value) materialGroup.Children.Add(matDiffuse);
            if (ButtonSpecularMaterial.IsChecked.Value) materialGroup.Children.Add(matSpecular);
            if (ButtonEmisiveMaterial.IsChecked.Value) materialGroup.Children.Add(matEmissive);

            if (materialGroup.Children.Count == 0) { materialGroup.Children.Add(matDiffuse); ButtonDiffuseMaterial.IsChecked = true; }
            geometry.Material = materialGroup;

            geometry.BackMaterial = ButtonBacksideDiffuseMaterial.IsChecked.Value ? matBackDiffuse : null;
        }



        private void Handler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { ButtonMaximize_Click(sender, e); return; }
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ButtonChangeTheme_Click(object sender, RoutedEventArgs e)
        {
            settings.LightTheme = !settings.LightTheme;
            setAppTheme();
        }

        void setAppTheme()
        {
            if (settings.LightTheme) App.SetTheme("ColorsLight.xaml");
            else App.SetTheme("Colors.xaml");
        }


        Point mLastPos;

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            camera.Scale(e.Delta > 0 ? 0.9 : 1.1);
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Pressed)
            {
                Point pos = Mouse.GetPosition(viewport);
                Point actualPos = new Point(pos.X - viewport.ActualWidth / 2, viewport.ActualHeight / 2 - pos.Y);
                double dx = actualPos.X - mLastPos.X, dy = actualPos.Y - mLastPos.Y;

                if (e.LeftButton == MouseButtonState.Pressed) camera.Move(dx, dy);
                if (e.RightButton == MouseButtonState.Pressed) camera.Orbit(dx, dy);

                mLastPos = actualPos;
            }
        }

        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.LeftShift)) { this.DragMove(); return; }

            Point pos = Mouse.GetPosition(viewport);
            mLastPos = new Point(pos.X - viewport.ActualWidth / 2, viewport.ActualHeight / 2 - pos.Y);
            ((IInputElement)sender).CaptureMouse();
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((IInputElement)sender).ReleaseMouseCapture();
        }

        private void filePanel_MouseEnter(object sender, MouseEventArgs e)
        {
            filePanel.Opacity = 1.0;
        }

        private void filePanel_MouseLeave(object sender, MouseEventArgs e)
        {
            filePanel.Opacity = settings.FilePanelIdleOpacity;
        }



        private void ButtonReloadModel_Click(object sender, RoutedEventArgs e)
        {
            clearView();
            if (lastFilename != null) loadFile(lastFilename);
        }

        private void ButtonSaveImage_Click(object sender, RoutedEventArgs e)
        {
            var sd = new Microsoft.Win32.SaveFileDialog() { Filter = "PNG Image|*.png" };
            if (sd.ShowDialog().Value) Utils.RenderElementToPNG(viewport3D, sd.FileName, settings.SaveImageDPI);
        }

        private void ButtonOpenExtApp_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(settings.ExternalApp)) { MessageBox.Show("No app specified in settings"); return; }
            System.Diagnostics.Process.Start(settings.ExternalApp, settings.ExternalAppArguments.Replace("$file$", lastFilename));
        }

        private void ButtonRotateZ_Click(object sender, RoutedEventArgs e)
        {
            addRotation(Axis.Z, settings.ModelRotationAngle);
        }

        private void ButtonRotateX_Click(object sender, RoutedEventArgs e)
        {
            addRotation(Axis.X, settings.ModelRotationAngle);
        }

        private void ButtonRotateY_Click(object sender, RoutedEventArgs e)
        {
            addRotation(Axis.Y, settings.ModelRotationAngle);
        }

        private void ButtonMaterial_Click(object sender, RoutedEventArgs e)
        {
            applyMaterials();
        }

        private void ButtonCenterCameraAtModel_Click(object sender, RoutedEventArgs e)
        {
            camera.TurnAt(modelCenter);
        }

        private void ButtonSwapNormals_Click(object sender, RoutedEventArgs e)
        {
            Utils.InvertNormals(mesh);
        }

        private void ButtonRecreateUnsmoothed_Click(object sender, RoutedEventArgs e)
        {
            Utils.RecreateUnsmoothed(mesh);
        }

        private void ButtonRemoveNormals_Click(object sender, RoutedEventArgs e)
        {
            mesh.Normals.Clear();
        }

        private void ButtonChangeVerticesOrder_Click(object sender, RoutedEventArgs e)
        {
            Utils.InvertVertexOrder(mesh);
        }

        void updateCameraModes()
        {
            camera.SelectType(ButtonSelectCamera.IsChecked.Value);
            camera.FOV = ButtonFishEyeFOV.IsChecked.Value ? settings.FishEyeFOV : settings.FOV;
        }

        private void ButtonSelectCamera_Click(object sender, RoutedEventArgs e)
        {
            updateCameraModes();
        }

        private void ButtonFishEyeFOV_Click(object sender, RoutedEventArgs e)
        {
            updateCameraModes();
        }

        Model3DGroup tempForAxes;
        void updateAxes()
        {
            if (!ButtonAxes.IsChecked.Value) modelgroup.Children.Remove(axesgroup);// axesgroup = tempForAxes;
            else modelgroup.Children.Add(tempForAxes);
        }

        GeometryModel3D tempForGround;
        void updateGround()
        {
            if (!ButtonGround.IsChecked.Value) modelgroup.Children.Remove(ground);
            else modelgroup.Children.Add(tempForGround);
        }

        private void ButtonAxes_Click(object sender, RoutedEventArgs e)
        {
            updateAxes();
        }

        private void ButtonGround_Click(object sender, RoutedEventArgs e)
        {
            updateGround();
        }

        void updateModelInfo()
        {
            if (ButtonShowModelInfo.IsChecked.Value) showModelInfo();
            else hideInfo();
        }

        private void ButtonShowModelInfo_Click(object sender, RoutedEventArgs e)
        {
            updateModelInfo();
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Close app and open settings.ini in default editor?\r\n(You have to associate ini files with Notepad++ or other text editor before using this option)", "Request", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            saveSettings();
            settings.StartProcess();
            Close();
        }

        
    }
}
