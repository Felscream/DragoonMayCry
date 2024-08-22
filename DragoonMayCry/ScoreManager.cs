using DragoonMayCry.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace DragoonMayCry
{
    public class ScoreManager
    {
        public StyleRank CurrentRank
        {
            get
            {
                return styleRankHandler.CurrentStyle.Value;
            }
        }

        public void OnLogout() => ResetScore();

        private StyleRankHandler styleRankHandler;
        public ScoreManager()
        {
            styleRankHandler = new ();
        }

        public void GoToNextRank()
        {
            styleRankHandler.GoToNextRank(true);
        }

        private void ResetScore()
        {
            styleRankHandler.Reset();
        }
        
    }
}
