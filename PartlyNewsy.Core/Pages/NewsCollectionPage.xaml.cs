using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PartlyNewsy.Models;
using Xamarin.Forms;

namespace PartlyNewsy.Core
{    
    public partial class NewsCollectionPage : ContentPage
    {
        public NewsCollectionPage()
        {
            InitializeComponent();
        }

        string categoryName;
        public string CategoryName
        {
            get => categoryName;
            set
            {
                categoryName = value;
                OnPropertyChanged(nameof(CategoryName));
            }
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            await GetData();
        }

        async Task GetData()
        {
            List<Article> articles = null;

            var svc = new NewsService();

            if (string.IsNullOrEmpty(CategoryName))
            {
                articles = await svc.GetTopNews();
            }
            else
            {
                articles = await svc.GetNewsByCategory(CategoryName);
            }

            newsList.ItemsSource = articles;
        }

        protected async void newsListItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection[0] is Article article)
            {
                var url = Uri.EscapeDataString(article.ArticleUrl);

                await Shell.Current.GoToAsync($"articledetail?articleUrl={url}", true);

                //newsList.SelectedItem = null;
            } 
        }        
    }
}
