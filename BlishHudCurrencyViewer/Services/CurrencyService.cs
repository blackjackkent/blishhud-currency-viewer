using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Blish_HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gw2Sharp.WebApi.V2.Models;
using BlishHudCurrencyViewer.Models;
using Microsoft.Xna.Framework;

namespace BlishHudCurrencyViewer.Services
{
    internal class CurrencyService
    {
        private Gw2ApiManager _apiManager;
        private SettingsManager _settingsManager;
        private Logger _logger;
        private List<Currency> _allInGameCurrencies;
        private List<SettingEntry<bool>> _availableCurrencySettings;
        private List<UserCurrency> _userAccountCurrencies;
        private List<SettingEntry> _selectedCurrencySettings;

        public event EventHandler AllGameCurrencyFetched;

        public CurrencyService(Gw2ApiManager apiManager, SettingsManager settingsManager, Logger logger) 
        {
            _apiManager = apiManager;
            _settingsManager = settingsManager;
            _logger = logger; 
            _selectedCurrencySettings = _settingsManager.ModuleSettings.Where(s => s.EntryKey.StartsWith("currency-setting-") && (s as SettingEntry<bool>)?.Value == true).ToList();

        }
        public async Task InitializeCurrencySettings()
        {
            try
            {
                var currencyResponse = await _apiManager.Gw2ApiClient.V2.Currencies.AllAsync();
                _allInGameCurrencies = currencyResponse.OrderBy(c => c.Name).ToList();
                _availableCurrencySettings = new List<SettingEntry<bool>>();
                _allInGameCurrencies.ForEach(c =>
                {
                    var setting = _settingsManager.ModuleSettings.DefineSetting(
                        "currency-setting-" + c.Id,
                        false,
                        () => c.Name
                    );
                    _availableCurrencySettings.Add(setting);
                });
            }
            catch (Exception e)
            {
                _logger.Warn(e.Message);
            }
        }
        public async Task<List<UserCurrency>> GetUserCurrencies()
        {
            if (!_apiManager.HasPermissions(new[] { TokenPermission.Account, TokenPermission.Wallet }))
            {
                _logger.Debug("User has incorrect permissions.");
                _userAccountCurrencies = new List<UserCurrency>();
                return _userAccountCurrencies;
            }
            try
            {
                var currencyResponse = await _apiManager.Gw2ApiClient.V2.Account.Wallet.GetAsync();
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
                _logger.Debug($"Loaded {_userAccountCurrencies.Count()} currencies.");
            }
            catch (Exception e)
            {
                _logger.Warn(e.Message);
                _userAccountCurrencies = new List<UserCurrency>();
            }
            return _userAccountCurrencies;
        }

        public bool Update(GameTime gameTime)
        {
            var currentSelectedSettings = _settingsManager.ModuleSettings.Where(s => s.EntryKey.StartsWith("currency-setting-") && (s as SettingEntry<bool>)?.Value == true).ToList();
            if (currentSelectedSettings != null && currentSelectedSettings.Count != _selectedCurrencySettings.Count)
            {
                _selectedCurrencySettings = currentSelectedSettings;
                return true;
            }
            return false;
        }
    }

}
