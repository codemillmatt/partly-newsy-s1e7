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
            if (!(e.CurrentSelection[0] is NewsCategory selectedCategory))
                return;

            if (!authService.IsLoggedIn())
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
            

            // THE BELOW ISN'T IN THE EPISODE AND IS BONUS CONTENT!!

            // Tell the App Shell to add a new interest tab
            var interestInfo = new InterestSubMessage
            {
                Category = newsCategory.CategoryName,
                Title = newsCategory.DisplayName
            };

            var msg = new AddInterestTabMessage { InterestInfo = interestInfo };

            MessagingCenter.Send(msg, AddInterestTabMessage.AddTabMessage);
        }

        async Task DeleteFavorite(NewsCategory newsCategory)
        {
            await Data.DeleteAsync<UserInterests>(
                newsCategory.CategoryName, DefaultPartitions.UserDocuments);

            var msg = new RemoveInterestTabMessage { InterestName = newsCategory.CategoryName };

            MessagingCenter.Send(msg, RemoveInterestTabMessage.RemoveTabMessage);
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
                    var favoriteCategory = allNewsCats.First(c => c.CategoryName == interest.NewsCategoryName);
                    favoriteCategory.IsFavorite = true;

                    // add the tabs
                    var interestInfo = new InterestSubMessage
                    {
                        Category = favoriteCategory.CategoryName,
                        Title = favoriteCategory.DisplayName
                    };

                    var msg = new AddInterestTabMessage { InterestInfo = interestInfo };

                    MessagingCenter.Send(msg, AddInterestTabMessage.AddTabMessage);
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
