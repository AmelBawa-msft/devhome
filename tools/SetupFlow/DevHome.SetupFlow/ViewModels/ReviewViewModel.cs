﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ViewModels;

public partial class ReviewViewModel : SetupPageViewModelBase
{
    private readonly IHost _host;

    [ObservableProperty]
    private IList<ReviewTabViewModelBase> _reviewTabs;

    [ObservableProperty]
    private ReviewTabViewModelBase _selectedReviewTab;

    [ObservableProperty]
    private bool _readAndAgree;

    public bool HasApplicationsToInstall => Orchestrator.GetTaskGroup<AppManagementTaskGroup>()?.SetupTasks.Any() == true;

    public bool RequiresTermsAgreement => HasApplicationsToInstall;

    public bool HasMSStoreApplicationsToInstall
    {
        get
        {
            var hasMSStoreApps = Orchestrator.GetTaskGroup<AppManagementTaskGroup>()?.SetupTasks.Any(task =>
            {
                var installTask = task as InstallPackageTask;
                return installTask?.IsFromMSStore == true;
            });

            return hasMSStoreApps == true;
        }
    }

    public bool HasTasksToSetUp => Orchestrator.TaskGroups.Any(g => g.SetupTasks.Any());

    public ReviewViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _host = host;

        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        PageTitle = StringResource.GetLocalized(StringResourceKey.ReviewPageTitle);
        CanGoToNextPage = false;
    }

    protected async override Task OnEachNavigateToAsync()
    {
        // Re-compute the list of tabs as it can change depending on the current selections
        ReviewTabs =
            Orchestrator.TaskGroups
            .Select(taskGroup => taskGroup.GetReviewTabViewModel())
            .Where(tab => tab?.HasItems == true)
            .ToList();
        SelectedReviewTab = ReviewTabs.FirstOrDefault();

        NextPageButtonToolTipText = HasTasksToSetUp ? null : StringResource.GetLocalized(StringResourceKey.ReviewNothingToSetUpToolTip);
        UpdateCanGoToNextPage();
        await Task.CompletedTask;
    }

    partial void OnReadAndAgreeChanged(bool value) => UpdateCanGoToNextPage();

    public void UpdateCanGoToNextPage()
    {
        CanGoToNextPage = HasTasksToSetUp && IsValidTermsAgreement();
        Orchestrator.NotifyNavigationCanExecuteChanged();
    }

    /// <summary>
    /// Validate if the terms agreement is required and checked
    /// </summary>
    /// <returns>True if terms agreement is valid, false otherwise.</returns>
    private bool IsValidTermsAgreement()
    {
        return !RequiresTermsAgreement || ReadAndAgree;
    }
}
