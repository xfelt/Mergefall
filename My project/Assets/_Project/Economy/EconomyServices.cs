using MergeSurvivor.Core;

namespace MergeSurvivor.Economy
{
    public enum CurrencyType
    {
        Soft,
        Premium,
        ProgressionResource
    }

    [System.Serializable]
    public sealed class AccountData
    {
        public int Soft;
        public int Premium;
        public int Resource;
    }

    public interface IInventoryService
    {
        int Get(CurrencyType type);
        void Add(GameReward reward);
        bool Spend(CurrencyType type, int amount);
    }

    public sealed class InventoryService : IInventoryService
    {
        private readonly AccountData _data;
        public InventoryService(AccountData data) => _data = data ?? new AccountData();
        public AccountData Data => _data;

        public int Get(CurrencyType type) => type switch
        {
            CurrencyType.Soft => _data.Soft,
            CurrencyType.Premium => _data.Premium,
            _ => _data.Resource
        };

        public void Add(GameReward reward)
        {
            _data.Soft += reward.SoftCurrency;
            _data.Premium += reward.PremiumCurrency;
            _data.Resource += reward.ProgressionResource;
        }

        public bool Spend(CurrencyType type, int amount)
        {
            if (amount <= 0) return false;
            var current = Get(type);
            if (current < amount) return false;
            switch (type)
            {
                case CurrencyType.Soft: _data.Soft -= amount; break;
                case CurrencyType.Premium: _data.Premium -= amount; break;
                default: _data.Resource -= amount; break;
            }
            return true;
        }
    }
}
