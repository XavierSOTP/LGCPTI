using LGC.PTI.Plugin.Client.ConfigPages.Plugin.General;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SoftSolute.Genetec.Plugin.Client.Host
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (FindName("ConfigPageView") is ConfigPageView view)
            {
                InitializeView(view);
            }
            else
            {
                var grid = Content as Grid;
                var pageView = grid?.Children
                    .OfType<ConfigPageView>()
                    .FirstOrDefault();

                if (pageView != null)
                    InitializeView(pageView);
            }
        }

        private void InitializeView(ConfigPageView view)
        {
            var configPage = new ConfigPage();

            #region Dummy Data Loading Method DO NOT DELETE
            /////****Once you copy the LoadDummyCardholderGroups method into the ConfigPage of the client, you can uncomment this line to load dummy data for testing purposes.****/////
            //configPage.LoadDummyCardholderGroups();
            #endregion

            view.DataContext = configPage;
        }
        #region Dummy Data Loading Method DO NOT DELETE 
        ////////////***Copy this dummy method into ConifgPage of the client so the host can populate the combobox with dummy data***///////////
        //public void LoadDummyCardholderGroups()
        //{
        //    var groups = Enumerable.Range(1, 50)
        //        .Select(i => new CardholderGroupItem
        //        {
        //            Guid = Guid.NewGuid(),
        //            Name = $"Cardholder Group {i:00}"
        //        })
        //        .ToList();

        //    groups.AddRange(
        //    [
        //        new CardholderGroupItem
        //{
        //    Guid = Guid.NewGuid(),
        //    Name = "Main Building Employees"
        //},
        //new CardholderGroupItem
        //{
        //    Guid = Guid.NewGuid(),
        //    Name = "Administration Team"
        //},
        //new CardholderGroupItem
        //{
        //    Guid = Guid.NewGuid(),
        //    Name = "External Contractors"
        //},
        //new CardholderGroupItem
        //{
        //    Guid = Guid.NewGuid(),
        //    Name = "Parking Employees"
        //},
        //new CardholderGroupItem
        //{
        //    Guid = Guid.NewGuid(),
        //    Name = "Security Administrators"
        //}
        //    ]);

        //    AllCardholderGroups = groups
        //        .OrderBy(x => x.Name)
        //        .ToList();

        //    OnPropertyChanged(nameof(AllCardholderGroups));
        //}
        #endregion
    }
}