using UnityEngine;

namespace MergeSurvivor.Data
{
    [CreateAssetMenu(menuName = "Merge Survivor/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id = "pawn_t1";
        [SerializeField] private string displayName = "Emerald";
        [SerializeField] private string familyId = "pawn";
        [SerializeField] private int tier = 1;
        [SerializeField] private Sprite icon;
        [SerializeField] private int combatPower = 5;

        public string Id => id;
        public string DisplayName => displayName;
        public string FamilyId => familyId;
        public int Tier => tier;
        public Sprite Icon => icon;
        public int CombatPower => combatPower;

        public void ConfigureRuntime(string newId, string newName, string newFamilyId, int newTier, int newPower)
        {
            id = newId;
            displayName = newName;
            familyId = newFamilyId;
            tier = newTier;
            combatPower = newPower;
        }
    }
}
