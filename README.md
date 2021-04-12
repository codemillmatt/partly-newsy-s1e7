Welcome back to Partly Cloudy! The show where you learn how to build a cloud-connected Xamarin mobile application. We start from nothing and don't quit until it's ready for the App Store!

So go [download all the code for this episode on GitHub](https://aka.ms/partly-cloudy-s1e7-github-masoucou)! (And if you haven't, [sign up for a free Azure account](https://azure.microsoft.com/free/?WT.mc_id=mobile-0000-masoucou), you'll need it to follow along.)

# Episode 7 Recap - Ctrl+S (App Center Data)

Data is important to almost every mobile app. Nah - I'm going to say data is important to every mobile app. 

And in this episode of Partly Cloudy you'll learn how to easily save data to Azure Cosmos DB using App Center. This way our user's favorite news categories will be available to them on any device they log in with!

## What Are We Going To Do?
We're going to save data to the cloud of course!

And... we're going to make sure it's available to read and write offline as well.

And make sure it's user partitioned. So that when using the app, you only see your data, and I only see my data.

And it will be as easy on the developer as possible!

What data will we persist do you ask? How about the preferences of specific news categories?

The workflow will then be like this:

1. Click on a news category
2. The app will prompt you to log in if you haven't already
3. The category info will save to Cosmos DB
4. Next time you log in to the app, that info will be pulled from Cosmos (or if you're not online - the onboard database representing the last time you were).
5. You'll see the category specific news on the first tab of the app automatically - in their own new top tab! _(And I'll show you how to do this in a future post.)_

### Picking Up From Last Time

I added some extra functionality from last time that's not shown in the video.


#### The My Interests Checkbox
The first is that whenever you click on an image in the **My Interests** page, a checkbox now appears.

This is done via a `SelectionChanged` handler on the `CollectionView`. Anytime the selection is changed - the event gets fired and it changes the `IsFavorite` property on the model bound to that particular element.

That `IsFavorite` property controls whether or not a `Label` is visible that displays a check box.

Checkout the code in `PartlyNewsy.Core` the `Pages` folder in the `MyInterestsPage.xaml.cs` and `Views` folder and `InterestsTile.xaml` to see more.

#### Azure Cosmos DB

The other thing I did was setup an Azure Cosmos DB instance.

Now this step is optional, but I like to have total control, so I went ahead and did it myself.

Here are some [docs on how to set one up](https://docs.microsoft.com/azure/cosmos-db/create-cosmosdb-resources-portal?WT.mc_id=mobile-0000-masoucou).

While you're doing that you can call your database anything you'd like - I called my `user-preferences`.

Up to you on what to call your collection too - I called mine `Info`.

But what you **do need to do** is have a partition key in that collection called: `PartitionKey`. (And that's for App Center integration.)

But like I said, if you don't want to go through this trouble - you don't have too - because App Center will take care of creating one for you if you want!

## App Center 💓 Azure Cosmos DB

One of the App Center Mobile Backend as a Service (MBaaS) features is just about seamless integration with Azure Cosmos DB.

App Center MBaaS provides slick, easy to use APIs for Auth and Push. And App Center Data is no exception.

In addition to all the [great features of Azure Cosmos DB](https://docs.microsoft.com/azure/cosmos-db/introduction?WT.mc_id=mobile-0000-masoucou#key-benefits) - such as world-wide replication, instant scaling, and generally just a fast, fast, fast database - App Center Data puts a nice abstraction over the Cosmos API to make things pretty easy to work with.

App Center data also gives you the ability to work with data offline and then performs the sync back to Cosmos when the user goes back online - all without you having to worry about it.

And ... App Center Data integrates nicely with App Center Auth.

This means that as soon as your user signs in - they get a special partition within Azure Cosmos DB (this is why you had to create the `PartitionKey` partition when creating the Cosmos DB account above).

All the data a signed-in user saves goes into their own partition that only they can access. App Center Data uses the fine grained security control that Azure Cosmos DB provides - but you don't have to worry about it. It's all done for you!

## Saving Data

And the App Center Data API is nice and easy to use too.

There's really three main parts involved in using App Center with Azure Cosmos DB.

Connecting App Center to Cosmos. Saving data. And reading data.

Let's take a look at each in turn.

### Connecting to Azure Cosmos DB

To connect App Center to Cosmos - you jump on into the App Center portal for the application you want to add data functionality to.

_Note: you will have to perform this for both your Android and iOS apps in App Center_

Click on the data menu from the left-hand menu, and you'll be presented with a screen like this.

![First screen in connecting app center to cosmos db](https://res.cloudinary.com/code-mill-technologies-inc/image/upload/bo_0px_solid_rgb:000000,c_scale,e_shadow:40,h_600/v1578501751/Capture-CosmosConnect_bzxyp9.jpg)

Then it'll walk you through either connecting to an existing Cosmos DB account or create a new one for you. [The directions here will help you out](https://docs.microsoft.com/appcenter/data/getting-started?WT.mc_id=mobile-0000-masoucou).

Once there, you're good to go.

### Saving Data

In order to save or read data, you'll need to add the [App Center Data NuGet package](https://www.nuget.org/packages/Microsoft.AppCenter.Data/) to all of the mobile projects in your solution, the iOS & Android head projects and the Forms core project.

Make sure you initialize it in the `AppCenter.Start` function by adding `typeof(Data)` - this will be in the `App.xaml.cs` file.

Then you get to the good stuff - saving.

#### Writing

Creating a record in Cosmos DB is as simple as calling the `Data.CreateAsync` function.

You pass in a primary key, the object that you want saved, and then which partition you want it saved in.

So for us, when saving a `UserInterests` class - it looks like this:

```language-csharp
await Data.CreateAsync<UserInterests>(newsCategory.CategoryName,
  new UserInterests { NewsCategoryName = newsCategory.CategoryName },
  DefaultPartitions.UserDocuments);
```

Assuming the user is signed-in, that `DefaultPartitions.UserDocuments` will make sure it gets saved in their personal Cosmos DB partition that only they can access!

There's also a `ReplaceAsync` function for [pdating a document](https://docs.microsoft.com/appcenter/sdk/data/xamarin?WT.mc_id=mobile-0000-masoucou#replace-upsert-a-document)

And of course - a `DeleteAsync` function for [deleting](https://docs.microsoft.com/appcenter/sdk/data/xamarin?WT.mc_id=mobile-0000-masoucou#delete-a-document).

### Reading Data

There are a couple more steps involved in reading data from App Center/Cosmos DB.

To get all documents of a particular type - you call the `ListAsync` function.

But the return isn't all the objects in a list. Rather it's a paged object - meaning you have to loop through it calling `GetNextPageAsync` to pull more data from the server.

This way you don't clog up the pipes, so to speak, pulling down a ton of data.

So pulling data will look like this:

```language-csharp
// Tell app center you want the data in list form
var dbResult = await Data.ListAsync<UserInterests>(
  DefaultPartitions.UserDocuments
);
var userInterests = new List<UserInterests>();

// deserialize the first page
userInterests.AddRange(dbResult.CurrentPage.Items.Select(i => i.DeserializedValue));

// check if there are additional pages
while (dbResult.HasNextPage)
{
  // grab that additional page
  await dbResult.GetNextPageAsync();

  // deserialize it
  userInterests.AddRange(dbResult.CurrentPage.Items.Select(i => i.DeserializedValue));
}
```

Here's more info on reading a [single document of data](https://docs.microsoft.com/appcenter/sdk/data/xamarin?WT.mc_id=mobile-0000-masoucou#reading-a-document), and then some on [reading lists of data](https://docs.microsoft.com/appcenter/sdk/data/xamarin?WT.mc_id=mobile-0000-masoucou#fetching-a-list-of-documents).

So those APIs aren't too bad. Especially considering that the power that they give you with offline and security.

## Summary

So there you have it - the ability to save data to the cloud, have it persist offline, and have it available only to specific users.

There will be a bonus blog coming that will show you how to implement getting the news for these _favorite categories_ and displaying them on the main page of the app. Look for that soon. (But the code already has it implemented - so go [download it and check it out](https://aka.ms/partly-cloudy-s1e7-github)!)

Until then - see you in the next episode!