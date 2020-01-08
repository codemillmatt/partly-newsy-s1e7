using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AppCenter.Data;
using PartlyNewsy.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace PartlyNewsy.Core
{
    public partial class AppShellPage : Shell
    {
        public AppShellPage()
        {
            InitializeComponent();

            MessagingCenter.Subscribe<AddInterestTabMessage>(this,
                AddInterestTabMessage.AddTabMessage,
                (msg) => AddNewsTab(msg)
           );

            MessagingCenter.Subscribe<RemoveInterestTabMessage>(this,
                RemoveInterestTabMessage.RemoveTabMessage,
                (msg) => RemoveNewsTab(msg));
        }

        void AddNewsTab(AddInterestTabMessage msg)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                newsTab.Items.Add(new ShellContent
                {
                    Content = new NewsCollectionPage { CategoryName = msg.InterestInfo.Category },
                    Title = msg.InterestInfo.Title
                });
            });
        }

        void RemoveNewsTab(RemoveInterestTabMessage msg)
        {
            // this has been working on Droid - need to test more on iOS
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var toRemove = newsTab.Items.SingleOrDefault(t => t.Title.Equals(msg.InterestName, StringComparison.OrdinalIgnoreCase));

                    if (toRemove != null)
                        newsTab.Items.Remove(toRemove);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            });
        }
    }
}
