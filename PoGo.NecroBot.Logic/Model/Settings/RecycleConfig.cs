namespace PoGo.NecroBot.Logic.Model.Settings
{
    public class RecycleConfig
    {
        public bool VerboseRecycling = true;
        public double RecycleInventoryAtUsagePercentage = 90.0;
        public bool RandomizeRecycle;
        public int RandomRecycleValue = 5;
        /*Amounts*/
        public int TotalAmountOfPokeballsToKeep = 120;
        public int TotalAmountOfPotionsToKeep = 80;
        public int TotalAmountOfRevivesToKeep = 60;
        public int TotalAmountOfBerriesToKeep = 50;

        public int MaxPokeballsToKeep = 50;
    }
}