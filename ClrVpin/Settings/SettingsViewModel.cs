﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Controls.Folder;
using ClrVpin.Models.Shared.Enums;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;

namespace ClrVpin.Settings;

[AddINotifyPropertyChangedInterface]
public class SettingsViewModel : IShowViewModel
{
    public SettingsViewModel()
    {
        PinballFolderModel = new GenericFolderTypeModel("Visual Pinball Executable", Settings.PinballFolder, false, folder => Settings.PinballFolder = folder);
        PinballContentTypeModels = Model.Settings.GetPinballContentTypes().Select(contentType => new ContentFolderTypeModel(contentType)).ToList();

        FrontendFolderModel = new GenericFolderTypeModel("PinballY/X Frontend Executable", Settings.FrontendFolder, false, folder => Settings.FrontendFolder = folder);
        FrontendContentTypeModels = Model.Settings.GetFrontendContentTypes().Select(contentType => new ContentFolderTypeModel(contentType)).ToList();

        BackupFolderModel = new GenericFolderTypeModel("Backup Root", Settings.BackupFolder, true, folder => Settings.BackupFolder = folder);

        CheckForUpdatesCommand = new ActionCommand(CheckForUpdates);
        ResetCommand = new ActionCommand(Reset);
        SaveCommand = new ActionCommand(Close);

        var vpxFolder = SettingsUtils.GetVpxFolder();
        var vpxTableFolder = SettingsUtils.GetTablesFolder();
        var pinballYFolder = SettingsUtils.GetPinballYFolder();
        var pinballXFolder = SettingsUtils.GetPinballXFolder();

        AutofillVpxFeature = CreateAutofillOption("Visual Pinball X", vpxFolder != null && vpxTableFolder != null, () => AutoAssignVpxFolders(vpxFolder, vpxTableFolder));
        AutofillPinballYFeature = CreateAutofillOption("Pinball Y", pinballYFolder != null, () => AutoAssignFrontendFolders(pinballYFolder, "Visual Pinball X"));
        AutofillPinballXFeature = CreateAutofillOption("Pinball X", pinballXFolder != null, () => AutoAssignFrontendFolders(pinballXFolder, "Visual Pinball"));
    }

    public FeatureType AutofillVpxFeature { get; }
    public FeatureType AutofillPinballYFeature { get; }
    public FeatureType AutofillPinballXFeature { get; }

    public GenericFolderTypeModel PinballFolderModel { get; }
    public List<ContentFolderTypeModel> PinballContentTypeModels { get; }

    public GenericFolderTypeModel FrontendFolderModel { get; }
    public List<ContentFolderTypeModel> FrontendContentTypeModels { get; }

    public GenericFolderTypeModel BackupFolderModel { get; }

    public ICommand CheckForUpdatesCommand { get; }

    public ICommand ResetCommand { get; }
    public ICommand SaveCommand { get; }

    public Models.Settings.Settings Settings { get; } = Model.Settings;

    public Window Show(Window parent)
    {
        _window = new MaterialWindowEx
        {
            Owner = parent,
            Content = this,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            Resources = parent.Resources,
            ContentTemplate = parent.FindResource("SettingsTemplate") as DataTemplate,
            ResizeMode = ResizeMode.NoResize,
            Title = "Settings",

            // limit height to activate the scroll bar, e.g. for use on lower resolution screens (aka full hd 1080px height)
            MaxHeight = Model.ScreenWorkArea.Height
        };

        _window.Show();
        _window.Closed += (_, _) => Model.SettingsManager.Write();

        return _window;
    }

    private static FeatureType CreateAutofillOption(string application, bool isEnabled, Action action)
    {
        var tip = "Automatically assign folders based on the installed application";
        if (!isEnabled)
            tip += $".. DISABLED BECAUSE '{application}' INSTALLATION WASN'T DETECTED";

        return FeatureOptions.CreateFeatureType(application, tip, isEnabled, action);
    }

    private void Close()
    {
        _window.Close();
    }

    private void AutoAssignVpxFolders(string vpxFolder, string vpxTableFolder)
    {
        PinballFolderModel.SetFolder(vpxFolder);

        // automatically assign folders based on the pinball root folder
        PinballContentTypeModels.ForEach(x =>
        {
            // for storage
            x.ContentType.Folder = vpxTableFolder;

            // for display
            x.Folder = x.ContentType.Folder;
        });
    }

    private void AutoAssignFrontendFolders(string frontendFolder, string subFolder)
    {
        FrontendFolderModel.SetFolder(frontendFolder);

        // automatically assign folders based on the frontend root folder
        FrontendContentTypeModels.ForEach(x =>
        {
            // for storage
            switch (x.ContentType.Category)
            {
                case ContentTypeCategoryEnum.Database:
                    x.ContentType.Folder = $@"{Settings.FrontendFolder}\Databases\{subFolder}";
                    break;
                case ContentTypeCategoryEnum.Media:
                    switch (x.ContentType.Enum)
                    {
                        case ContentTypeEnum.InstructionCards:
                        case ContentTypeEnum.FlyerImagesBack:
                        case ContentTypeEnum.FlyerImagesFront:
                        case ContentTypeEnum.FlyerImagesInside1:
                        case ContentTypeEnum.FlyerImagesInside2:
                        case ContentTypeEnum.FlyerImagesInside3:
                        case ContentTypeEnum.FlyerImagesInside4:
                        case ContentTypeEnum.FlyerImagesInside5:
                        case ContentTypeEnum.FlyerImagesInside6:
                            x.ContentType.Folder = $@"{Settings.FrontendFolder}\Media\{x.ContentType.Description}";
                            break;
                        default:
                            x.ContentType.Folder = $@"{Settings.FrontendFolder}\Media\{subFolder}\{x.ContentType.Description}";
                            break;
                    }

                    break;
            }

            // for display
            x.Folder = x.ContentType.Folder;
        });
    }

    private void Reset()
    {
        Model.SettingsManager.Reset();
        Close();
    }

    private void CheckForUpdates()
    {
        // automatically disable pre-release check if update checks are disabled
        if (!Settings.EnableCheckForUpdatesAutomatically)
            Settings.EnableCheckForUpdatesPreRelease = false;
    }


    private Window _window;
}