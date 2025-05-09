﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    public partial class MainWindow : Window
    {
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
            settings.IsEmissiveEnabled = ButtonEmissiveMaterial.IsChecked.Value;
            settings.IsBackDiffuseEnabled = ButtonBacksideDiffuseMaterial.IsChecked.Value;
            settings.IsOrthoCameraEnabled = ButtonSelectCamera.IsChecked.Value;
            settings.IsFishEyeModeEnabled = ButtonFishEyeFOV.IsChecked.Value;
            settings.IsAxesEnabled = ButtonAxes.IsChecked.Value;
            settings.IsGroundEnabled = ButtonGround.IsChecked.Value;

            settings.Save();
        }

        void loadSettings()
        {
            settings.Load();

            Utils.SetWindowStyle(this, settings.UseSystemWindowStyle, StackPanelSystemButtons);

            if (settings.GetParseErrorsCount() > 0) Utils.MessageWindow("Settings loader warnings:\r\n\r\n" + String.Join("\r\n", settings.GetParseErrors().ToArray()));
            setAppTheme();
            this.Left = settings.LocationX;
            this.Top = settings.LocationY;
            this.Width = settings.Width;
            this.Height = settings.Height;
            this.WindowState = settings.WindowState;
            FilePanelColumn.Width = new GridLength(settings.FilePanelWidth);
            filePanel.SortFilesByExtensions = settings.SortFilesByExtensions;
            filePanel.Path = settings.SelectedPath;
            filePanel.Refresh();

            ButtonDiffuseMaterial.IsChecked = settings.IsDiffuseEnabled;
            ButtonSpecularMaterial.IsChecked = settings.IsSpecularEnabled;
            ButtonEmissiveMaterial.IsChecked = settings.IsEmissiveEnabled;
            ButtonBacksideDiffuseMaterial.IsChecked = settings.IsBackDiffuseEnabled;
            ButtonSelectCamera.IsChecked = settings.IsOrthoCameraEnabled;
            ButtonFishEyeFOV.IsChecked = settings.IsFishEyeModeEnabled;
            ButtonAxes.IsChecked = settings.IsAxesEnabled;
            ButtonGround.IsChecked = settings.IsGroundEnabled;

            settings.FOV = Utils.MinMax(settings.FOV, 5.0, 160.0);
            settings.FishEyeFOV = Utils.MinMax(settings.FishEyeFOV, 5.0, 160.0);
            settings.CameraInitialShift = Utils.MinMax(settings.CameraInitialShift, 0.5, 10.0);
            settings.ModelRotationAngle = Utils.MinMax(settings.ModelRotationAngle, 5.0, 180.0);
            settings.WireframeEdgeScale = Utils.MinMax(settings.WireframeEdgeScale, 0.001, 0.9);

            updateCameraModes();
            scene.IsAxesVisible = ButtonAxes.IsChecked.Value;
            scene.IsGroundVisible = ButtonGround.IsChecked.Value;

            scene.CreateMaterials(settings.DiffuseColor.Color, settings.SpecularColor.Color, settings.EmissiveColor.Color, settings.BackDiffuseColor.Color, settings.SpecularPower);
            applyMaterials();

            scene.RemoveAllLights();
            if (settings.IsAmbientLightEnabled) scene.CreateAmbientLight(settings.AmbientLightColor.Color);
            if (settings.IsDirectionalLight1Enabled) scene.CreateDirectionalLight(settings.DirectionalLight1Color.Color, settings.DirectionalLight1Dir);
            if (settings.IsDirectionalLight2Enabled) scene.CreateDirectionalLight(settings.DirectionalLight2Color.Color, settings.DirectionalLight2Dir);
            if (settings.IsDirectionalLight3Enabled) scene.CreateDirectionalLight(settings.DirectionalLight3Color.Color, settings.DirectionalLight3Dir);

            scene.CreateAxes(settings.AxesLength);
            scene.CreateGround(settings.GroundRectangle, settings.GroundCheckerSize, settings.GroundDiffuseColor.Color, settings.GroundEmissiveColor.Color);
        }
    }
}
