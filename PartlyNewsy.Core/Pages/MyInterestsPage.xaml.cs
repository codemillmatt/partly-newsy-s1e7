using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AppCenter.Auth;

using PartlyNewsy.Models;
using Xamarin.Forms;
using System.Linq;
using Microsoft.AppCenter.Data;

namespace PartlyNewsy.Core
{
    public partial class MyInterestsPage : ContentPage
    {
        AuthenticationService authService = new AuthenticationService();

        List<NewsCategory> AllNewsCategories { get; set; }

        public MyInterestsPage()
        {
            InitializeComponent();
           
            AllNewsCategories = new List<NewsCategory>();

            newsCategories.ItemsSource = AllNewsCategories;                                    
        }

        protected async void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!authService.IsLoggedIn())
                return;

            if (!(e.CurrentSelection[0] is NewsCategory selectedCategory))
                return;

            selectedCategory.IsFavorite = !selectedCategory.IsFavorite;

            SetCheckbox(selectedCategory);

            if (selectedCategory.IsFavorite)
                await CreateFavorite(selectedCategory);
            else
                await DeleteFavorite(selectedCategory);
        }

        async Task CreateFavorite(NewsCategory newsCategory)
        {
            await Data.CreateAsync<UserInterests>(newsCategory.CategoryName,
                new UserInterests { NewsCategoryName = newsCategory.CategoryName },
                DefaultPartitions.UserDocuments);
        }

        async Task DeleteFavorite(NewsCategory newsCategory)
        {
            await Data.DeleteAsync<UserInterests>(
                newsCategory.CategoryName, DefaultPartitions.UserDocuments);
        }

        async Task<List<NewsCategory>> GetUsersNewsCategories()
        {
            try
            {
                var loggedIn = authService.IsLoggedIn();

                if (!loggedIn)
                    return new List<NewsCategory>();

                var dbResult = await Data.ListAsync<UserInterests>(
                    DefaultPartitions.UserDocuments
                );

                var allNewsCats = new AllNewsCategories();
                var userInterests = new List<UserInterests>();

                userInterests.AddRange(dbResult.CurrentPage.Items.Select(i => i.DeserializedValue));

                while (dbResult.HasNextPage)
                {
                    await dbResult.GetNextPageAsync();

                    userInterests.AddRange(dbResult.CurrentPage.Items.Select(i => i.DeserializedValue));
                }

                foreach (var interest in userInterests)
                {
                    allNewsCats.First(c => c.CategoryName == interest.NewsCategoryName)
                        .IsFavorite = true;
                }

                return allNewsCats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                return new List<NewsCategory>();
            }

        }

        void SetCheckbox(NewsCategory selectedCategory)
        {
            var temporaryCats = new List<NewsCategory>();

            foreach (var item in AllNewsCategories)
            {
                if (item.CategoryName == selectedCategory.CategoryName)
                    item.IsFavorite = selectedCategory.IsFavorite;

                temporaryCats.Add(item);
            }

            AllNewsCategories = null;
            AllNewsCategories = new List<NewsCategory>(temporaryCats);
            newsCategories.ItemsSource = AllNewsCategories;
        }
       
        protected async void SignIn_Clicked(object sender, EventArgs e)
        {
            var success = await authService.Login();

            if (!success)
            {
                await DisplayAlert("Warning!", "There was an issue logging in", "OK");
                return;
            }

            var userFavorites = await GetUsersNewsCategories();
            
            AllNewsCategories = new List<NewsCategory>(userFavorites);

            newsCategories.ItemsSource = AllNewsCategories;
            //newsCategories.ItemsSource = new AllNewsCategories();
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var isLoggedIn = authService.IsLoggedIn();

            if (isLoggedIn && AllNewsCategories.Count == 0)
            {
                var userFavorites = await GetUsersNewsCategories();

                AllNewsCategories = new List<NewsCategory>(userFavorites);
                newsCategories.ItemsSource = AllNewsCategories;
            }
        }

    }
}
