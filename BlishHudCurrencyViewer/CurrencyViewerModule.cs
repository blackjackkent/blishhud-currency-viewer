﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using BlishHudCurrencyViewer.Services;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;

namespace BlishHudCurrencyViewer
{
    [Export(typeof(Module))]
    public class CurrencyViewerModule : Module
    {
        private static readonly Logger Logger = Logger.GetLogger<CurrencyViewerModule>();

        #region Service Managers
        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;
        internal ApiPollingService PollingService;
        internal WindowService WindowService;
        internal CurrencyService CurrencyService;

        #endregion

        [ImportingConstructor]
        public CurrencyViewerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {  
        }

        protected override void Initialize()
        {
            Gw2ApiManager.SubtokenUpdated += OnApiSubTokenUpdated;
        }

        private void OnApiSubTokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            PollingService?.Invoke();
        }
        
        protected override async Task LoadAsync()
        {
            try
            {
                WindowService = new WindowService(ContentsManager, SettingsManager);
                CurrencyService = new CurrencyService(Gw2ApiManager, SettingsManager, Logger);

                WindowService.InitializeIfNotExists();
                await CurrencyService.InitializeCurrencySettings();

                PollingService = new ApiPollingService();
                PollingService.ApiPollingTrigger += delegate
                {
                    Task.Run(() =>
                    {
                        RefreshWindow();
                    });
                };
            }
            catch (Exception e)
            {
                Logger.Warn(e, e.Message);
            }
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            base.OnModuleLoaded(e);
            _cornerIcon = new CornerIcon()
            {
                Icon = ContentsManager.GetTexture("coins.png"),
                BasicTooltipText = $"{Name}",
                Parent = GameService.Graphics.SpriteScreen
            };

            _cornerIcon.Click += delegate
            {
                WindowService.Toggle();
            };
            RefreshWindow();
        }
        protected override void Update(GameTime gameTime)
        {
            WindowService.Update(gameTime);
            PollingService?.Update(gameTime);
            var shouldRedraw = CurrencyService.Update(gameTime);
            if (shouldRedraw)
            {
                RefreshWindow();
            }
        }

        protected async void RefreshWindow()
        {
            var userCurrencies = await CurrencyService.GetUserCurrencies();
            WindowService.RedrawWindowContent(userCurrencies);
        }

        protected override void Unload()
        {
            Gw2ApiManager.SubtokenUpdated -= OnApiSubTokenUpdated;
            PollingService?.Dispose();
            WindowService?.Dispose();
            _cornerIcon?.Dispose();

            ModuleInstance = null;
        }

        internal static CurrencyViewerModule ModuleInstance; 
        private CornerIcon _cornerIcon;
    }
}
