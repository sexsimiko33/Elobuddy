using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;

namespace SexsiTwitch
{
    /**
    * 
    * Not my work, all credits to Definitely not Kappa
    * 
    */

    internal static class PassiveManager
    {
        public static List<PassiveEnemy> PassiveEnemies = new List<PassiveEnemy>();

        public static string BuffName = "TwitchDeadlyVenom";

        public static string StackName = "twitch_poison_counter_0";
        public static string StackEndName = ".troy";

        private static float BaseEDmg => new[] { 20f, 35f, 50f, 65f, 80f }[Program.E.Level - 1];

        private static float PerStackBonus
            => 5f * Program.E.Level + 10f + Player.Instance.FlatPhysicalDamageMod * 0.25f +
               Player.Instance.TotalMagicalDamage * 0.2f;

        public static void Init()
        {
            GameObject.OnCreate += GameObject_OnCreate;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            Clear();
            if (!sender.Name.Contains(StackName)) return;
            var owner = ObjectManager.Get<Obj_AI_Base>().OrderBy(o => o.Distance(sender)).FirstOrDefault(HasPassive);
            if (owner != null)
                AddNewPassive(owner, sender.Name);
        }

        public static float EDamage(this Obj_AI_Base target)
        {
            var stacktarget = PassiveEnemies.FirstOrDefault(e => e.Target.IdEquals(target));
            if (!Program.E.IsLearned || stacktarget == null)
                return 0;

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                BaseEDmg + PerStackBonus * stacktarget.StackCount);
        }

        public static bool HasPassive(this Obj_AI_Base target)
        {
            return target.HasBuff(BuffName);
        }

        public static int PassiveCount(this Obj_AI_Base target)
        {
            var t = PassiveEnemies.FirstOrDefault(a => a.Target.IdEquals(target));
            return t?.StackCount ?? 0;
        }

        private static int _parseInt(string str)
        {
            var str1 = str.Replace(StackName, "").Replace(StackEndName, "");
            return int.Parse(str1);
        }

        public static void AddNewPassive(Obj_AI_Base target, string name)
        {
            var p = new PassiveEnemy(target, name);
            PassiveEnemies.RemoveAll(t => t.Target.IdEquals(target));
            PassiveEnemies.Add(p);
        }

        public static void Clear()
        {
            PassiveEnemies.RemoveAll(t => !t.Target.HasPassive());
        }

        public class PassiveEnemy
        {
            public string CurrentStackName;
            public float EndTick;
            public Obj_AI_Base Target;

            public PassiveEnemy(Obj_AI_Base target, string passiveName)
            {
                Target = target;
                CurrentStackName = passiveName;
                EndTick = Core.GameTickCount + 6000;
            }

            public float EDamage => Target.EDamage();

            public float CurrentTick
            {
                get
                {
                    var b = Target.GetBuff(BuffName);
                    return b != null ? b.EndTime - Game.Time * 1000 : EndTick - Core.GameTickCount;
                }
            }

            public int StackCount => !Target.HasPassive() ? 0 : _parseInt(CurrentStackName);

            public bool Ended => CurrentTick <= 0 || !Target.HasPassive() || Target.IsDead || !Target.IsValid;

            public bool WillEnd => CurrentTick <= 75 + Game.Ping;

            public bool Killable => Target.EDamage() >= Target.Health;

            public bool IsFullyStacked => StackCount >= 6;
        }
    }
}
