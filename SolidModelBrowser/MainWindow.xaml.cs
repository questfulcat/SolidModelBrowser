using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace SolidModelBrowser
{
    public partial class MainWindow : Window
    {
        const string version = "0.6";

        MeshGeometry3D meshBase, meshWireframe;
        Point3D modelCenter = new Point3D();
        Point3D modelPointsAvgCenter = new Point3D();
        Vector3D modelSize = new Vector3D();

        string lastFilename;

        UCamera camera;

        DispatcherTimer loadingTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100), IsEnabled = false };

        Settings settings = new Settings();

        public MainWindow()
        {
            InitializeComponent();
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; // to avoid "." "," mess while parsing
            
            ButtonReloadModel.Click += (s, e) => { clearView(); loadFile(lastFilename); };
            ButtonSaveImage.Click += (s, e) => Utils.SaveImagePNG(viewport3D, settings.SaveImageDPI);
            ButtonOpenExtApp.Click += (s, e) => Utils.RunExternalApp(settings.ExternalApp, settings.ExternalAppArguments, lastFilename);
            ButtonRotateX.Click += (s, e) => addRotation(settings.ModelRotationAngle, 0, 0);
            ButtonRotateY.Click += (s, e) => addRotation(0, settings.ModelRotationAngle, 0);
            ButtonRotateZ.Click += (s, e) => addRotation(0, 0, settings.ModelRotationAngle);
            ButtonDiffuseMaterial.Click += (s, e) => applyMaterials();
            ButtonSpecularMaterial.Click += (s, e) => applyMaterials();
            ButtonEmissiveMaterial.Click += (s, e) => applyMaterials();
            ButtonBacksideDiffuseMaterial.Click += (s, e) => applyMaterials();
            ButtonSelectCamera.Click += (s, e) => updateCameraModes();
            ButtonCenterCameraAtModelGC.Click += (s, e) => camera.TurnAt(modelCenter);
            ButtonCenterCameraAtModelMC.Click += (s, e) => camera.TurnAt(modelPointsAvgCenter);
            ButtonFishEyeFOV.Click += (s, e) => updateCameraModes();
            ButtonAxes.Click += (s, e) => updateAxes();
            ButtonGround.Click += (s, e) => updateGround();
            ButtonSwapNormals.Click += (s, e) => { Utils.InvertNormals(meshBase); updateMeshes(true); };
            ButtonRemoveNormals.Click += (s, e) => { meshBase.Normals.Clear(); updateMeshes(true); };
            ButtonChangeVerticesOrder.Click += (s, e) => { Utils.InvertVertexOrder(meshBase); updateMeshes(true); };
            ButtonRecreateUnsmoothed.Click += (s, e) => { meshBase = Utils.UnsmoothMesh(meshBase); updateMeshes(true); };
            ButtonWireframe.Click += (s, e) => updateMeshes(false);
            ButtonShowModelInfo.Click += (s, e) => updateModelInfo();
            ButtonSettings.Click += (s, e) => runSettings();
            ButtonExport.Click += (s, e) => export();

            ButtonChangeTheme.Click += (s, e) => { settings.LightTheme = !settings.LightTheme; setAppTheme(); };
            ButtonMinimize.Click += (s, e) => WindowState = WindowState.Minimized;
            ButtonMaximize.Click += (s, e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            ButtonClose.Click += (s, e) => Close();

            sliderSliceHeight.ValueChanged += (s, e) => slice();

            filePanel.IsIgnoringLetterKeysNavigation = true;
            filePanel.Extensions = new List<string>();
            foreach (Import importer in Import.Imports) filePanel.Extensions.AddRange(importer.Extensions);
            filePanel.MouseEnter += (s, e) => filePanel.Opacity = 1.0;
            filePanel.MouseLeave += (s, e) => filePanel.Opacity = settings.FilePanelIdleOpacity;
            filePanel.SelectionChanged += (s, e) => clearView();
            filePanel.FileNavigated += (s, filename) => loadFile(filename);

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
                        
            camera = new UCamera(viewport3D);

            tempForAxes = axesgroup;
            modelgroup.Children.Remove(axesgroup);
            tempForGround = ground;
            modelgroup.Children.Remove(ground);

            loadingTimer.Tick += LoadingTimer_Tick;

            loadSettings();
            this.Closing += (s, e) => saveSettings();

            // set max window size to avoid taskbar overlay
            var scrsize = Utils.GetCurrentScreenSize(this);
            MaxWidth = settings.MaxWidth < 1 ? scrsize.Width : Math.Max(settings.MaxWidth, MinWidth);
            MaxHeight = settings.MaxHeight < 1 ? scrsize.Height : Math.Max(settings.MaxHeight, MinHeight);

            showInfo($"Solid Model Browser v {version}\r\nSTL, OBJ, 3MF, PLY, GCODE formats supported\r\n\r\nquestfulcat 2025 (C)\r\nhttps://github.com/questfulcat/solidmodelbrowser\r\nDistributed under MIT License\r\n\r\nF1 - keymap help");
            this.ContentRendered += windowRendered;
        }

        void windowRendered(object s, EventArgs a)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 2 && File.Exists(args[1]) && Import.SelectImporter(args[1])) filePanel.SelectFile(args[1], true);
            this.ContentRendered -= windowRendered;
        }
        

        void showInfo(string info) { textBlockInfo.Text = info; infoContainer.Visibility = Visibility.Visible; }
        void hideInfo() => infoContainer.Visibility = Visibility.Collapsed;
        void showModelInfo() => showInfo($"File: {lastFilename}\r\n\r\nVertices: {mesh.Positions.Count}\r\nTriangles: {mesh.TriangleIndices.Count / 3}\r\nNormals: {mesh.Normals.Count}\r\n\r\nSize X:{modelSize.X.ToString("0.00")}, Y:{modelSize.Y.ToString("0.00")}, Z:{modelSize.Z.ToString("0.00")}\r\nGeometric Center X:{modelCenter.X.ToString("0.00")}, Y:{modelCenter.Y.ToString("0.00")}, Z:{modelCenter.Z.ToString("0.00")}\r\nPoints Average Center X:{modelPointsAvgCenter.X.ToString("0.00")}, Y:{modelPointsAvgCenter.Y.ToString("0.00")}, Z:{modelPointsAvgCenter.Z.ToString("0.00")}");


        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                //textBlockProgress.Text = Import.Progress.ToString();
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

                if (settings.IgnoreOriginalNormals) Import.Normals.Clear();
                meshBase = Utils.FillMeshFromImport();
                meshWireframe = null;
                if (settings.UnsmoothAfterLoading) meshBase = Utils.UnsmoothMesh(meshBase);
                Utils.FillMesh(meshBase, mesh);
                Utils.GetModelCenterAndSize(meshBase, out modelCenter, out modelPointsAvgCenter, out modelSize);
                camera.LookCenter = settings.DefaultLookAtModelPointsAvgCenter ? modelPointsAvgCenter : modelCenter;
                camera.DefaultPosition(modelSize.Length, settings.CameraInitialShift);

                if (Import.CurrentImport.InitialXRotationNeeded) addRotation(90.0, 0, 0);
                if (ButtonShowModelInfo.IsChecked.Value) showModelInfo();
                else hideInfo();

                initSlicer();
            }
            catch (Exception exc)
            {
                loadingTimer.Stop();
                clearView();
                showInfo(exc.Message);
            }
        }
                

        Task currentTask = null;
        void loadFile(string filename)
        {
            if (!File.Exists(filename)) return;
            lastFilename = null;
            try
            {
                if (currentTask?.Status == TaskStatus.Running)
                {
                    Import.StopFlag = true;
                    currentTask.Wait(3000);
                }
                currentTask?.Dispose();

                if (!Import.SelectImporter(filename)) { clearView(); return; }

                currentTask = new Task(() =>
                {
                    try
                    {
                        Import.CurrentImport.Load(filename);
                    }
                    catch (Exception exc) { Import.ExceptionMessage = exc.Message; }
                });

                lastFilename = filename;
                loadingTimer.Start();
                showInfo("loading...");

                currentTask.Start();
            }
            catch (Exception exc)
            {
                showInfo(exc.Message);
            }
        }

        void addRotation(double ax, double ay, double az)
        {
            Utils.RotateMesh(meshBase, settings.DefaultLookAtModelPointsAvgCenter ? modelPointsAvgCenter : modelCenter, ax, ay, az);
            Utils.GetModelCenterAndSize(meshBase, out modelCenter, out modelPointsAvgCenter, out modelSize);
            initSlicer();
            updateMeshes(true);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string CAS = Utils.GetKeyboardModifiers();
            
            if (CAS == "") // no modifier keys
            {
                double ra = settings.ModelRotationAngle;
                if (e.Key == Key.A) addRotation(0, 0, ra);
                if (e.Key == Key.D) addRotation(0, 0, -ra);
                if (e.Key == Key.S) addRotation(ra, 0, 0);
                if (e.Key == Key.W) addRotation(-ra, 0, 0);
                if (e.Key == Key.E) addRotation(0, ra, 0);
                if (e.Key == Key.Q) addRotation(0, -ra, 0);

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
                if (e.Key == Key.F2) { ButtonDiffuseMaterial.Toggle(); applyMaterials(); }
                if (e.Key == Key.F3) { ButtonSpecularMaterial.Toggle(); applyMaterials(); }
                if (e.Key == Key.F4) { ButtonEmissiveMaterial.Toggle(); applyMaterials(); }
                if (e.Key == Key.F5) { clearView(); loadFile(lastFilename); }
                if (e.Key == Key.F6) Utils.SaveImagePNG(viewport3D, settings.SaveImageDPI);
                if (e.Key == Key.F7) Utils.RunExternalApp(settings.ExternalApp, settings.ExternalAppArguments, lastFilename);
                if (e.Key == Key.F8) { ButtonSelectCamera.Toggle(); camera.SelectType(ButtonSelectCamera.IsChecked.Value); }
                if (e.Key == Key.C) camera.TurnAt(modelCenter);
                if (e.Key == Key.M) camera.TurnAt(modelPointsAvgCenter);
                if (e.Key == Key.O) { ButtonAxes.Toggle(); updateAxes(); }
                if (e.Key == Key.P) { ButtonGround.Toggle(); updateGround(); }
                if (e.Key == Key.I) { ButtonShowModelInfo.Toggle(); updateModelInfo(); }
            }

            if (CAS == "C") // CTRL
            {
                if (e.Key == Key.F) { meshBase = Utils.UnsmoothMesh(meshBase); updateMeshes(true); }
                if (e.Key == Key.W) { ButtonWireframe.Toggle(); updateMeshes(false); }
                if (e.Key == Key.S) export();
            }
        }
        
        void clearView()
        {
            if (meshBase == null) return;
            meshBase.TriangleIndices.Clear();
            meshBase.Positions.Clear();
            meshBase.Normals.Clear();
            ButtonWireframe.IsChecked = false;
            updateMeshes(false);
            transform.Children.Clear();
            hideInfo();
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
            if (ButtonEmissiveMaterial.IsChecked.Value) materialGroup.Children.Add(matEmissive);

            if (materialGroup.Children.Count == 0) { materialGroup.Children.Add(matDiffuse); ButtonDiffuseMaterial.IsChecked = true; }
            geometry.Material = materialGroup;

            geometry.BackMaterial = ButtonBacksideDiffuseMaterial.IsChecked.Value ? matBackDiffuse : null;
        }



        private void Handler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; return; }
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        void setAppTheme()
        {
            if (settings.LightTheme) App.SetTheme("ColorsLight.xaml");
            else App.SetTheme("Colors.xaml");
            filePanel.ExtensionsColors.Clear();
            if (settings.ColorizeFiles) { Import.FillColorsDictionary(filePanel.ExtensionsColors, settings.LightTheme); filePanel.Refresh(); }
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



        void updateCameraModes()
        {
            camera.SelectType(ButtonSelectCamera.IsChecked.Value);
            camera.FOV = ButtonFishEyeFOV.IsChecked.Value ? settings.FishEyeFOV : settings.FOV;
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

        void updateModelInfo()
        {
            if (ButtonShowModelInfo.IsChecked.Value) showModelInfo();
            else hideInfo();
        }

        void runSettings()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.propertyPanel.SetObject(settings);
            this.Effect = new BlurEffect() { Radius = 4.0 };
            settingsWindow.ShowDialog();
            this.Effect = null;
            if (settingsWindow.WindowResult == SettingsWindowResult.ResetDefaults) { settings = new Settings(); }
            saveSettings();
            loadSettings();
            if (settingsWindow.WindowResult == SettingsWindowResult.OpenEditor) { settings.StartProcess(); Close(); }
        }

        void updateMeshes(bool forceUpdateWireframe)
        {
            var wfmode = ButtonWireframe.IsChecked.Value;
            if (wfmode)
            {
                if (meshWireframe == null || forceUpdateWireframe)
                    meshWireframe = Utils.ConvertToWireframe(meshBase, settings.WireframeEdgeScale);
            }
            else meshWireframe = null;
            Utils.FillMesh(wfmode ? meshWireframe : meshBase, mesh);
            slice();
        }

        void initSlicer()
        {
            skipslice = true;
            sliderSliceHeight.Maximum = modelCenter.Z + modelSize.Z / 2;
            sliderSliceHeight.Minimum = modelCenter.Z - modelSize.Z / 2;
            sliderSliceHeight.Value = sliderSliceHeight.Maximum;
            sliderSliceHeight.LargeChange = 0.2;
            skipslice = false;
        }

        //int slicescount = 0;
        bool skipslice = false;
        void slice()
        {
            if (skipslice) return;
            Utils.SliceMesh(ButtonWireframe.IsChecked.Value ? meshWireframe : meshBase, mesh, sliderSliceHeight.Value);
            if (mesh.TriangleIndices.Count == 0) { mesh.TriangleIndices.Add(0); }
            //showInfo($"ms: {Utils.sw.ElapsedMilliseconds}\r\nticks: {Utils.sw.ElapsedTicks}\r\nheight: {sliderSliceHeight.Value}, slicescount {++slicescount}, tris: {mesh.TriangleIndices.Count}");
        }

        void export()
        {
            string exporters = string.Join(",", Export.Exports.Select(e => e.Description).OrderBy(e => e));
            string r = Utils.MessageWindow("Select format", this, exporters, Orientation.Vertical);
            var exp = Export.Exports.FirstOrDefault(e => e.Description == r);
            if (exp == null) return;
            var sfd = new System.Windows.Forms.SaveFileDialog() { DefaultExt = exp.Extension, Filter = $"{exp.Description}|*.{exp.Extension}" };
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            try
            {
                MeshGeometry3D m = mesh;
                if (exp.InitialXRotationNeeded) { m = mesh.Clone(); Utils.RotateMesh(m, settings.DefaultLookAtModelPointsAvgCenter ? modelPointsAvgCenter : modelCenter, -90, 0, 0); }
                exp.Save(m, sfd.FileName);
                showInfo($"Exported to file {sfd.FileName}");
            }
            catch (Exception exc) { Utils.MessageWindow($"Export failed with message\r\n{exc.Message}"); }
        }
    }
}
