﻿#region

using System;
using System.Drawing;
using System.Linq;
using SharpDX;

#endregion

namespace LeagueSharp.Common
{
    public class TargetSelector
    {
        public enum TargetingMode
        {
            LowHP,
            MostAD,
            MostAP,
            Closest,
            NearMouse,
            AutoPriority,
            LessAttack,
            LessCast,
        }

        private static double _lasttick;

        private static readonly string[] ap =
        {
            "Ahri", "Akali", "Anivia", "Annie", "Brand", "Cassiopeia", "Diana",
            "FiddleSticks", "Fizz", "Gragas", "Heimerdinger", "Karthus", "Kassadin", "Katarina", "Kayle", "Kennen",
            "Leblanc", "Lissandra", "Lux", "Malzahar", "Mordekaiser", "Morgana", "Nidalee", "Orianna", "Ryze", "Sion",
            "Swain", "Syndra", "Teemo", "TwistedFate", "Veigar", "Viktor", "Vladimir", "Xerath", "Ziggs", "Zyra",
            "Velkoz"
        };

        private static readonly string[] sup =
        {
            "Blitzcrank", "Janna", "Karma", "Leona", "Lulu", "Nami", "Sona",
            "Soraka", "Thresh", "Zilean"
        };

        private static readonly string[] tank =
        {
            "Amumu", "Chogath", "DrMundo", "Galio", "Hecarim", "Malphite",
            "Maokai", "Nasus", "Rammus", "Sejuani", "Shen", "Singed", "Skarner", "Volibear", "Warwick", "Yorick", "Zac",
            "Nunu", "Taric", "Alistar", "Garen", "Nautilus", "Braum"
        };

        private static readonly string[] ad =
        {
            "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "KogMaw",
            "MissFortune", "Quinn", "Sivir", "Talon", "Tristana", "Twitch", "Urgot", "Varus", "Vayne", "Zed", "Jinx",
            "Yasuo", "Lucian"
        };

        private static readonly string[] bruiser =
        {
            "Darius", "Elise", "Evelynn", "Fiora", "Gangplank", "Jayce",
            "Pantheon", "Irelia", "JarvanIV", "Jax", "Khazix", "LeeSin", "Nocturne", "Olaf", "Poppy", "Renekton",
            "Rengar", "Riven", "Shyvana", "Trundle", "Tryndamere", "Udyr", "Vi", "MonkeyKing", "XinZhao", "Aatrox",
            "Rumble", "Shaco", "MasterYi"
        };

        public Obj_AI_Hero Target;
        private bool _drawcircle;
        private Obj_AI_Hero _maintarget;
        private TargetingMode _mode;
        private float _range;
        private bool _update = true;

        public TargetSelector(float range, TargetingMode mode)
        {
            _range = range;
            _mode = mode;

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (MenuGUI.IsChatOpen || ObjectManager.Player.Spellbook.SelectedSpellSlot != SpellSlot.Unknown) return;

            if (args.WParam == 1) // LMouse
            {
                switch (args.Msg)
                {
                    case 257:
                        foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
                        {
                            if (hero.IsValidTarget() &&
                                SharpDX.Vector2.Distance(Game.CursorPos.To2D(), hero.ServerPosition.To2D()) < 300)
                            {
                                Target = hero;
                                _maintarget = hero;
                                Game.PrintChat("TargetSelector: New main target: " + _maintarget.BaseSkinName);
                            }
                        }
                        break;
                }
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead && _drawcircle && Target != null && Target.IsVisible && !Target.IsDead)
            {
                Drawing.DrawCircle(Target.Position, 125, System.Drawing.Color.White);
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Environment.TickCount > _lasttick + 100)
            {
                _lasttick = Environment.TickCount;
                if (!_update) return;
                if (_maintarget == null)
                {
                    GetNormalTarget();
                }
                else
                {
                    if (Geometry.Distance(_maintarget) > _range)
                    {
                        GetNormalTarget();
                    }
                    else
                    {
                        if (_maintarget.IsValidTarget())
                        {
                            Target = _maintarget;
                        }
                        else
                        {
                            GetNormalTarget();
                        }
                    }
                }
            }
        }

        private void GetNormalTarget()
        {
            Obj_AI_Hero newtarget = null;
            if (_mode != TargetingMode.AutoPriority)
            {
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget() && Geometry.Distance(target) <= _range)
                    {
                        if (newtarget == null)
                        {
                            newtarget = target;
                        }
                        else
                        {
                            switch (_mode)
                            {
                                case TargetingMode.LowHP:
                                    if (target.Health < newtarget.Health)
                                    {
                                        newtarget = target;
                                    }
                                    break;
                                case TargetingMode.MostAD:
                                    if (target.BaseAttackDamage + target.FlatPhysicalDamageMod <
                                        newtarget.BaseAttackDamage + newtarget.FlatPhysicalDamageMod)
                                    {
                                        newtarget = target;
                                    }
                                    break;
                                case TargetingMode.MostAP:
                                    if (target.FlatMagicDamageMod < newtarget.FlatMagicDamageMod)
                                    {
                                        newtarget = target;
                                    }
                                    break;
                                case TargetingMode.Closest:
                                    if (Geometry.Distance(target) < Geometry.Distance(newtarget))
                                    {
                                        newtarget = target;
                                    }
                                    break;
                                case TargetingMode.NearMouse:
                                    if (SharpDX.Vector2.Distance(Game.CursorPos.To2D(), target.Position.To2D()) <
                                        SharpDX.Vector2.Distance(Game.CursorPos.To2D(), newtarget.Position.To2D()))
                                    {
                                        newtarget = target;
                                    }
                                    break;
                                case TargetingMode.LessAttack:
                                    if ((target.Health - DamageLib.CalcPhysicalDmg(target.Health, target)) <
                                        (newtarget.Health - DamageLib.CalcPhysicalDmg(newtarget.Health, newtarget)))
                                    {
                                        newtarget = target;
                                    }
                                    break;
                                case TargetingMode.LessCast:
                                    if ((target.Health - DamageLib.CalcMagicDmg(target.Health, target)) <
                                        (target.Health - DamageLib.CalcMagicDmg(newtarget.Health, newtarget)))
                                    {
                                        newtarget = target;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            else
            {
                newtarget = AutoPriority();
            }
            Target = newtarget;
        }


        private Obj_AI_Hero AutoPriority()
        {
            Obj_AI_Hero autopriority = null;
            var prio = 5;
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (target != null && target.IsValidTarget() && Geometry.Distance(target) <= _range)
                {
                    if (autopriority == null)
                    {
                        autopriority = target;
                        prio = FindPrioForTarget(target.BaseSkinName);
                    }
                    else
                    {
                        if (FindPrioForTarget(target.BaseSkinName) < prio)
                        {
                            autopriority = target;
                            prio = FindPrioForTarget(target.BaseSkinName);
                        }
                        else if (FindPrioForTarget(target.BaseSkinName) == prio)
                        {
                            if (!(target.Health < autopriority.Health)) continue;
                            autopriority = target;
                            prio = FindPrioForTarget(target.BaseSkinName);
                        }
                    }
                }
            }
            return autopriority;
        }

        private static int FindPrioForTarget(string baseskinname)
        {
            if (ap.Contains(baseskinname))
                return 2;
            if (ad.Contains(baseskinname))
                return 1;
            if (sup.Contains(baseskinname))
                return 3;
            if (bruiser.Contains(baseskinname))
                return 4;
            if (tank.Contains(baseskinname))
                return 5;
            return 5;
        }

        public void SetDrawCircleOfTarget(bool draw)
        {
            _drawcircle = draw;
        }

        public void OverrideTarget(Obj_AI_Hero newtarget)
        {
            Target = newtarget;
            _update = false;
        }

        public void DisableTargetOverride()
        {
            _update = true;
        }

        public float GetRange()
        {
            return _range;
        }

        public void SetRange(float range)
        {
            _range = range;
        }

        public TargetingMode GetTargetingMode()
        {
            return _mode;
        }

        public void SetTargetingMode(TargetingMode mode)
        {
            _mode = mode;
        }

        public override string ToString()
        {
            return "Target: " + Target.BaseSkinName + "Range: " + _range + "Mode: " + _mode;
        }
    }

    /// <summary>
    /// Simple target selector that selects the hero that will die faster.
    /// </summary>
    public static class SimpleTs
    {
        private static Menu _config;
        private static Obj_AI_Hero _selectedTarget;

        internal static Obj_AI_Hero SelectedTarget
        {
            get { return (_config != null && _config.Item("FocusSelected").GetValue<bool>() ? _selectedTarget : null); }
        }
        public enum DamageType
        {
            Magical,
            Physical,
            True,
        }

        static SimpleTs()
        {
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (_selectedTarget.IsValidTarget() && _config != null && _config.Item("FocusSelected").GetValue<bool>() && _config.Item("SelTColor").GetValue<Circle>().Active)
                Utility.DrawCircle(_selectedTarget.Position, 150, _config.Item("SelTColor").GetValue<Circle>().Color, 3, 23);
        }

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget()).OrderByDescending(h => h.Distance(Game.CursorPos)))
                {
                    if (enemy.Distance(Game.CursorPos) < 300)
                        if (_selectedTarget != null && enemy.NetworkId == _selectedTarget.NetworkId)
                            _selectedTarget = null;
                        else
                            _selectedTarget = enemy;
                }
            }
        }

        internal static float GetPriority(Obj_AI_Hero hero)
        {
            var p = 1;
            if (_config != null && _config.Item("SimpleTS" + hero.BaseSkinName + "Priority") != null)
                p = _config.Item("SimpleTS" + hero.BaseSkinName + "Priority").GetValue<Slider>().Value;

            switch (p)
            {
                case 2:
                    return 1.5f;
                case 3:
                    return 2f;
                case 4:
                    return 2.5f;
                case 5:
                    return 3f;
                default:
                    return 1f;
            }
        }

        public static void AddToMenu(Menu Config)
        {
            _config = Config;
            Config.AddItem(new MenuItem("FocusSelected", "Focus selected target").SetShared().SetValue(true));
            Config.AddItem(new MenuItem("SelTColor", "Selected target color").SetShared().SetValue(new Circle(true, System.Drawing.Color.Red)));
            Config.AddItem(new MenuItem("Sep", "").SetShared());
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Team != ObjectManager.Player.Team))
                    Config.AddItem(new MenuItem("SimpleTS" + enemy.BaseSkinName + "Priority", enemy.BaseSkinName).SetShared().SetValue(new Slider(1, 5, 1)));
            
        }

        public static Obj_AI_Hero GetTarget(float range, DamageType damageType)
        {
            Obj_AI_Hero bestTarget = null;
            var bestRatio = 0f;

            if (_selectedTarget.IsValidTarget() && (range < 0 && Orbwalking.InAutoAttackRange(_selectedTarget) || ObjectManager.Player.Distance(_selectedTarget) < range ))
                return _selectedTarget;

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsValidTarget() && (range < 0 && Orbwalking.InAutoAttackRange(hero) || ObjectManager.Player.Distance(hero) < range))
                {
                    var damage = 0f;

                    switch (damageType)
                    {
                        case DamageType.Magical:
                            damage = (float)DamageLib.CalcMagicDmg(100, hero);
                            break;
                        case DamageType.Physical:
                            damage = (float)DamageLib.CalcPhysicalDmg(100, hero);
                            break;
                        case DamageType.True:
                            damage = 100;
                            break;
                    }

                    var ratio = damage / (1 + hero.Health) * GetPriority(hero);

                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        bestTarget = hero;
                    }
                }
            }

            return bestTarget;
        }
    }
}