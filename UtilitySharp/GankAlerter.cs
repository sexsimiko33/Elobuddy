
using System;
using System.Drawing;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace UtilitySharp
{
    public class GankAlerter
    {
        private Menu menu;

        private CheckBox Enable;

        private Slider Value;

        public GankAlerter(Menu mainMenu)
        {
            menu = mainMenu.AddSubMenu("Gank Alerter");

            menu.AddGroupLabel("Draw Champions");

            Enable = menu.Add("enable", new CheckBox("Enable Gang alerter"));

            Value = menu.Add("value", new Slider("Distance to draw", 2000, 600, 3000));

            foreach (var obj in EntityManager.Heroes.Enemies)
            {
                menu.Add("draw" + obj.ChampionName, new CheckBox("Draw " + obj.ChampionName));
            }

            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            foreach (var obj in EntityManager.Heroes.Enemies)
            {
                //Console.WriteLine(menu["draw" + obj.BaseSkinName].Cast<CheckBox>().CurrentValue);      
                if (obj != null && Enable.CurrentValue && menu["draw" + obj.ChampionName].Cast<CheckBox>().CurrentValue &&
                    obj.IsValidTarget(Value.CurrentValue) && Player.Instance.CountAllyChampionsInRange(Value.CurrentValue) < 2 && Player.Instance.Distance(obj) > 500)
                {
                    Drawing.DrawLine(Player.Instance.Position.WorldToScreen(), obj.Position.WorldToScreen(), 5, Color.Azure);
                    //Drawing.DrawText(Player.Instance.Position.WorldToScreen().Extend(obj, 100), Color.Red, string.Format("WARNING {0} incoming !", obj.ChampionName), 65);
                }
            }
        }
    }
}
