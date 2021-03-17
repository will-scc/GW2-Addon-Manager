﻿using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using GW2_Addon_Manager.App.Configuration;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using System.Threading;
using System.Globalization;

namespace GW2_Addon_Manager
{
    /// <summary>
    /// Code-behind for OpeningView.xaml.
    /// </summary>
    public partial class OpeningView : Page
    {
        static string releases_url = "https://github.com/fmmmlee/GW2-Addon-Manager/releases";
        static string UpdateNotificationFile = "updatenotification.txt";

        private readonly IConfigurationManager _configurationManager;
        private readonly PluginManagement _pluginManagement;

        /// <summary>
        /// This constructor deals with creating or expanding the configuration file, setting the DataContext, and checking for application updates.
        /// </summary>
        public OpeningView()
        {
            DataContext = OpeningViewModel.GetInstance;

            _configurationManager = new ConfigurationManager();
            var configuration = new Configuration(_configurationManager);
            configuration.CheckSelfUpdates();
            configuration.DetermineSystemType();
            _pluginManagement = new PluginManagement(_configurationManager);
            _pluginManagement.DisplayAddonStatus();

            InitializeComponent();
            //update notification
            if (File.Exists(UpdateNotificationFile))
            {
                Process.Start(releases_url);
                File.Delete(UpdateNotificationFile);
            }
        }


        /**** What Add-On Is Selected ****/
        /// <summary>
        /// Takes care of description page text updating
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void addOnList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            AddonInfoFromYaml selected = OpeningViewModel.GetInstance.AddonList[addons.SelectedIndex];
            OpeningViewModel.GetInstance.DescriptionText = selected.description;
            OpeningViewModel.GetInstance.DeveloperText = selected.developer;
            OpeningViewModel.GetInstance.AddonWebsiteLink = selected.website;

            OpeningViewModel.GetInstance.DeveloperVisibility = Visibility.Visible;
        }

        /***************************** NAV BAR *****************************/
        private void TitleBar_MouseHeld(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                Application.Current.MainWindow.DragMove();
        }

        

        //race condition with processes
        private void close_clicked(object sender, RoutedEventArgs e)
        {
            SelfUpdate.startUpdater();
            System.Windows.Application.Current.Shutdown();
        }

        private void minimize_clicked(object sender, RoutedEventArgs e)
        {
            (this.Parent as Window).WindowState = WindowState.Minimized;
        }

        /*****************************   ***   *****************************/


        //just calls PluginManagement.ForceRedownload(); and then update_button_clicked
        private void RedownloadAddons(object sender, RoutedEventArgs e)
        {
            if(_pluginManagement.ForceRedownload())
                update_button_clicked(sender, e);
        }


        /***** UPDATE button *****/
        private void update_button_clicked(object sender, RoutedEventArgs e)
        {
            //If bin folder doesn't exist then LoaderSetup intialization will fail.
            if (_configurationManager.UserConfig.BinFolder == null)
            {
                MessageBox.Show("Unable to locate Guild Wars 2 /bin/ or /bin64/ folder." + Environment.NewLine + "Please verify Game Path is correct.",
                                "Unable to Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<AddonInfoFromYaml> selectedAddons = new List<AddonInfoFromYaml>();

            //the d3d9 wrapper is installed by default and hidden from the list displayed to the user, so it has to be added to this list manually
            AddonInfoFromYaml wrapper = AddonYamlReader.getAddonInInfo("d3d9_wrapper");
            wrapper.folder_name = "d3d9_wrapper";
            selectedAddons.Add(wrapper);

            foreach (AddonInfoFromYaml addon in OpeningViewModel.GetInstance.AddonList.Where(add => add.IsSelected == true))
            {
                selectedAddons.Add(addon);
            }

            Application.Current.Properties["Selected"] = selectedAddons;

            this.NavigationService.Navigate(new Uri("UI/UpdatingPage/UpdatingView.xaml", UriKind.Relative));
        }

        /***** Hyperlink Handler *****/
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void SelectDirectoryBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var pathSelectionDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            
            if (pathSelectionDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                //Important that GamePath property is set first as this updates _configurationManager
                //If _configurationManager.UserConfig.GamePath is invalid, _configurationManager.UserConfig.BinFolder will not be set
                OpeningViewModel.GetInstance.GamePath = pathSelectionDialog.FileName;
                Configuration configuration = new Configuration(_configurationManager);
                configuration.DetermineSystemType();
            }
        }
    }
}
