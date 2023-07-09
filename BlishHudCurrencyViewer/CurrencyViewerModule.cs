using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using BlishHudCurrencyViewer.Models;
using BlishHudCurrencyViewer.Services;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Color = Microsoft.Xna.Framework.Color;

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
            PollingService.Invoke();
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
                    Task.Run(async () =>
                    {
                        var userCurrencies = await CurrencyService.GetUserCurrencies();
                        WindowService.RedrawWindowContent(userCurrencies);
                    });
                };
            }
            catch (Exception e)
            {
            }
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            base.OnModuleLoaded(e);
            _cornerIcon = new CornerIcon()
            {
                Icon = GameService.Content.DatAssetCache.GetTextureFromAssetId(156753),
                BasicTooltipText = $"{Name}",
                Parent = GameService.Graphics.SpriteScreen
            };

            _cornerIcon.Click += delegate
            {
                WindowService.Toggle();
            };
        }

        protected override void Update(GameTime gameTime)
        {
            WindowService.Update(gameTime);
            PollingService?.Update(gameTime);
        }

        protected override void Unload()
        {
            Gw2ApiManager.SubtokenUpdated -= OnApiSubTokenUpdated;
            PollingService?.Dispose();
            WindowService?.Dispose();

            ModuleInstance = null;
        }

        internal static CurrencyViewerModule ModuleInstance; 
        private CornerIcon _cornerIcon;
    }
}
