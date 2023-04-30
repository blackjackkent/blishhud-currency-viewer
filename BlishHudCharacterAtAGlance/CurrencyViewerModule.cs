using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Overlay.UI.Views;
using Blish_HUD.Settings;
using BlishHudCurrencyViewer.Models;
using BlishHudCurrencyViewer.Services;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Color = Microsoft.Xna.Framework.Color;
using BlishHudCurrencyViewer.Views;

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

        private async void OnApiSubTokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            PollingService.Invoke();
        }
        
        protected override async Task LoadAsync()
        {
            try
            {
                PollingService = new ApiPollingService();
                PollingService.ApiPollingTrigger += delegate
                {
                    Task.Run(async () =>
                    {
                        await GetUserCurrencies();
                    });
                };
                await GetAllInGameCurrencies();
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
                Icon = GameService.Content.DatAssetCache.GetTextureFromAssetId(2594222),
                BasicTooltipText = $"{Name}",
                Parent = GameService.Graphics.SpriteScreen
            };

            _cornerIcon.Click += delegate
            {
                InitializeWindowIfNotExists();
                _window.ToggleWindow();
            };
        }

        private void InitializeWindowIfNotExists()
        {
            if (_window == null)
            {
                var backgroundTexture = GameService.Content.DatAssetCache.GetTextureFromAssetId(155985);
                var currencyViewerWindow = new StandardWindow(ContentsManager.GetTexture("empty.png"), new Rectangle(0, 0, 300, 400), new Rectangle(10, 20, 280, 360))
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Title = "User Currency",
                    BackgroundColor = new Color(0, 0, 0, 0.8f),
                    Emblem = null,
                    SavesPosition = true,
                    Id = $"{nameof(CurrencyViewerModule)}_38d37290-b5f9-447d-97ea-45b0b50e5f56",
                };
                _window = currencyViewerWindow;
            }
            RedrawWindowContent();
        }

        private void RedrawWindowContent()
        {
            ResetDisplayData(); 
            var selectedCurrencySettings = _currencySelectionSettings.Where(s => s.Value == true && s.EntryKey.StartsWith("currency-setting-")).ToList();
            if (_userAccountCurrencies == null || _userAccountCurrencies.Count() == 0 || selectedCurrencySettings.Count() == 0)
            {
                _window.Height = 200;
                _window.Width = 360;
                _window.HeightSizingMode = SizingMode.Standard;
                _window.WidthSizingMode = SizingMode.Standard;
                _noCurrenciesSelectedText = new Label
                {
                    Text = "You have not yet selected any currencies to track! Go to BlishHud's CurrencyViewer module settings to select some.",
                    Parent = _window,
                    Width = 300,
                    Height = 200,
                    WrapText = true,
                    VerticalAlignment = VerticalAlignment.Top
                };
                return;
            }
            for (int i = 0; i < selectedCurrencySettings.Count(); i++)
            {
                _window.HeightSizingMode = SizingMode.AutoSize;
                _window.WidthSizingMode = SizingMode.AutoSize;
                _window.AutoSizePadding = new Point
                {
                    X = 70,
                    Y = 0
                };
                var currency = selectedCurrencySettings[i];
                var userCurrency = _userAccountCurrencies.Find(c => "currency-setting-" + c.CurrencyId == currency.EntryKey);
                if (userCurrency == null)
                {
                    userCurrency = new UserCurrency
                    {
                        CurrencyName = currency.DisplayName,
                        CurrencyQuantity = 0
                    };
                }
                var nameLabel = new Label
                {
                    Text = currency.DisplayName,
                    Parent = _window,
                    Top = i * 20,
                    Left = 0,
                    AutoSizeWidth = true
                };
                var quantityLabel = new Label
                {
                    Text = userCurrency.CurrencyQuantity.ToString(),
                    Parent = _window,
                    Top = i * 20,
                    Left = 200,
                    AutoSizeWidth = true
                };
                _displayData.Add(new UserCurrencyDisplayData
                {
                    CurrencyDisplayName = userCurrency.CurrencyName,
                    Name = nameLabel,
                    Quantity = quantityLabel
                });
            }
        }

        private void ResetDisplayData()
        {
            if (_noCurrenciesSelectedText != null)
            {
                _noCurrenciesSelectedText.Dispose();
                _noCurrenciesSelectedText = null;
            }

            if (_displayData == null)
            {
                _displayData = new List<UserCurrencyDisplayData>();
            }
            _displayData.ForEach(d =>
            {
                d.Name.Dispose();
                d.Quantity.Dispose();
            });
            _displayData.Clear();
        }

        protected override void Update(GameTime gameTime)
        {
            InitializeWindowIfNotExists();
            PollingService?.Update(gameTime);
        }

        protected override void Unload()
        {
            Gw2ApiManager.SubtokenUpdated -= OnApiSubTokenUpdated;
            PollingService?.Dispose();

            ModuleInstance = null;
        }

        private async Task GetAllInGameCurrencies()
        {
            try
            {
                var currencyResponse = await Gw2ApiManager.Gw2ApiClient.V2.Currencies.AllAsync();
                _allInGameCurrencies = currencyResponse.OrderBy(c => c.Name).ToList();
                _currencySelectionSettings = new List<SettingEntry<bool>>();
                _allInGameCurrencies.ForEach(c =>
                    {
                        var setting = SettingsManager.ModuleSettings.DefineSetting(
                            "currency-setting-" + c.Id,
                            false,
                            () => c.Name
                        );
                        _currencySelectionSettings.Add(setting);
                    });
                PollingService.Invoke();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }

        private async Task GetUserCurrencies()
        {
            if (!Gw2ApiManager.HasPermissions(new[] { TokenPermission.Account, TokenPermission.Wallet }))
            {
                Logger.Debug("User has incorrect permissions.");
                _userAccountCurrencies = null;
                return;
            }
                try
                {
                    var currencyResponse = await Gw2ApiManager.Gw2ApiClient.V2.Account.Wallet.GetAsync();
                    var userCurrencyList = currencyResponse.ToList();
                    _userAccountCurrencies = new List<UserCurrency>();
                    userCurrencyList.ForEach(uc =>
                    {
                        var currencyData = _allInGameCurrencies.FirstOrDefault(c => c.Id == uc.Id);
                        var userCurrency = new UserCurrency
                        {
                            CurrencyId = uc.Id,
                            CurrencyName = currencyData?.Name,
                            CurrencyQuantity = uc.Value
                        };
                        _userAccountCurrencies.Add(userCurrency);
                    });
                    Logger.Debug($"Loaded {_userAccountCurrencies.Count()} currencies.");
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }
        }

        internal static CurrencyViewerModule ModuleInstance; 
        private CornerIcon _cornerIcon;
        List<Currency> _allInGameCurrencies;
        List<UserCurrency> _userAccountCurrencies;
        List<SettingEntry<bool>> _currencySelectionSettings;
        private StandardWindow _window;
        private List<UserCurrencyDisplayData> _displayData;
        private Label _noCurrenciesSelectedText;
    }
}
