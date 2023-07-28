using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Blish_HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework;
using BlishHudCurrencyViewer.Models;
using Blish_HUD.Settings;

namespace BlishHudCurrencyViewer.Services
{
    public class WindowService : IDisposable
    {
        private StandardWindow _window;
        private ContentsManager _contentsManager;
        private SettingsManager _settingsManager;
        private bool _isVisible;
        private List<UserCurrencyDisplayData> _displayData;
        private Label _descriptionText;

        public WindowService(ContentsManager contentsManager, SettingsManager settingsManager)
        {
            _contentsManager = contentsManager;
            _settingsManager = settingsManager;
        }

        public void Toggle() { 
            _isVisible = !_isVisible; 
        }

        private bool ShouldHideWindow()
        {      
            return !GameService.GameIntegration.Gw2Instance.Gw2IsRunning ||
                !GameService.GameIntegration.Gw2Instance.IsInGame ||
                !GameService.Gw2Mumble.IsAvailable ||
                GameService.Gw2Mumble.UI.IsMapOpen;
        }

        public void Update(GameTime gameTime) {
            InitializeIfNotExists();
            if (ShouldHideWindow())
            {
                _window.Hide();
                return;
            }
            if (_isVisible)
            {
                _window.Show();
                return;
            } 
            _window.Hide();
        }

        public void InitializeIfNotExists()
        {
            if (_window == null)
            {
                var currencyViewerWindow = new StandardWindow(_contentsManager.GetTexture("empty.png"), new Rectangle(0, 0, 300, 400), new Rectangle(10, 20, 280, 360))
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    BackgroundColor = new Color(0, 0, 0, 0.6f),
                    Title = "",
                    Emblem = _contentsManager.GetTexture("coins.png"),
                    SavesPosition = true,
                    CanCloseWithEscape = false,
                    Id = $"{nameof(CurrencyViewerModule)}_38d37290-b5f9-447d-97ea-45b0b50e5f56",
                    Opacity = 0.6f
                    
                };
                _window = currencyViewerWindow;
                _window.Hidden += delegate
                {
                    if (!ShouldHideWindow()) {
                        _isVisible = false;
                    }
                };
            }
        }

        public void RedrawWindowContent(List<UserCurrency> userCurrencies)
        {
            ResetDisplayData();
            var selectedCurrencySettings = _settingsManager.ModuleSettings.Where(s => s.EntryKey.StartsWith("currency-setting-") && (s as SettingEntry<bool>)?.Value == true).ToList();
            if (userCurrencies == null || userCurrencies.Count() == 0 || selectedCurrencySettings.Count() == 0)
            {
                _window.Height = 120;
                _window.Width = 280;
                _window.HeightSizingMode = SizingMode.Standard;
                _window.WidthSizingMode = SizingMode.Standard;
                _descriptionText = new Label
                {
                    Text = "You have not yet selected any currencies to track! Go to BlishHud's CurrencyViewer module settings to select some.",
                    Parent = _window,
                    Width = 300,
                    Height = 120,
                    WrapText = true,
                    VerticalAlignment = VerticalAlignment.Top
                };
                return;
            }

            _window.HeightSizingMode = SizingMode.AutoSize;
            for (int i = 0; i < selectedCurrencySettings.Count(); i++)
            {
                _window.AutoSizePadding = new Point
                {
                    X = 70,
                    Y = 0
                };
                var currency = selectedCurrencySettings[i];
                var userCurrency = userCurrencies.Find(c => "currency-setting-" + c.CurrencyId == currency.EntryKey);
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
                    Width = 150,
                    WrapText = true
                };
                var quantityLabel = new Label
                {
                    Text = userCurrency.CurrencyQuantity.ToString("N0"),
                    Parent = _window,
                    Top = i * 20,
                    Left = 180,
                    AutoSizeWidth = true,
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
            if (_descriptionText != null)
            {
                _descriptionText.Dispose();
                _descriptionText = null;
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

        public void Dispose()
        {
            ResetDisplayData();
            _window.Dispose();
        }
    }
}
