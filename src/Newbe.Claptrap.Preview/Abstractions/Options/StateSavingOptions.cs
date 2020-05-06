using System;

namespace Newbe.Claptrap.Preview.Abstractions.Options
{
    public class StateSavingOptions
    {
        public TimeSpan? SavingWindowTime { get; set; }
        public int? SavingWindowVersionLimit { get; set; }

        /// <summary>
        /// save state when claptrap deactivated or not
        /// </summary>
        public bool SaveWhenDeactivateAsync { get; set; }
    }
}