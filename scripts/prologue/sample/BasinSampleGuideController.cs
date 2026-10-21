using Godot;
using Knight.Abilities;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Prologue.Seamless;
using Knight.Settings;

namespace Knight.Prologue.Sample;

// Read-only, session-local teaching. Never grants progress, supply, health or completion.
public partial class BasinSampleGuideController : Node
{
    public int Step { get; private set; }
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private BasinSampleGuideView _view;
    private PrologueSector _sector;
    private BellBackLaborer _enemy;
    private StaticBody2D _catch, _shelf, _floor;
    private PrologueSupplyCache _supply;
    private Area2D _camp;
    private MechanicalClawController _claw;
    private PlayerActionModeController _modes;
    private PlayerEnergyController _energy;
    private PlayerShieldController _shield;
    private PlayerInputReader _input;
    private ulong _observeAfter;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _level = GetParent<PrologueLevelController>();
        _flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
        _claw = _level.Player.GetNode<MechanicalClawController>("MechanicalClaw");
        _modes = _level.Player.GetNode<PlayerActionModeController>("ActionModes");
        _energy = _level.Player.GetNode<PlayerEnergyController>("Energy");
        _shield = _level.Player.GetNode<PlayerShieldController>("Shield");
        _input = _level.Player.GetNode<PlayerInputReader>("Input");
        _view = new BasinSampleGuideView { Name = "View", Visible = false };
        AddChild(_view);
        _flow.SessionStarted += Reset;
        _level.RecoveryRequested += OnRecovery;
        _level.RecoveryCompleted += OnRecovery;
        Reset();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_flow)) _flow.SessionStarted -= Reset;
        if (!IsInstanceValid(_level)) return;
        _level.RecoveryRequested -= OnRecovery;
        _level.RecoveryCompleted -= OnRecovery;
    }

    private void OnRecovery(string reason) => Reset();

    private void Reset()
    {
        Step = 0;
        // Checkpoint enemy reset is deferred and player resume crosses a physics boundary.
        _observeAfter = Engine.GetPhysicsFrames() + 3;
        _view.Visible = false;
        _view.SetWorldFocus(null, Vector2.Zero, string.Empty);
        _view.PresentCombat(null, false);
    }

    public override void _Process(double delta)
    {
        bool playing = _flow.State == PrologueGameFlowState.Playing && !GetTree().Paused &&
            Engine.GetPhysicsFrames() >= _observeAfter && _level.Player.IsVisibleInTree() &&
            _level.Player.IsPhysicsProcessing() &&
            _level.CurrentSector == PrologueSectorId.CanyonBasin;
        _view.Visible = playing;
        if (!playing) { _view.PresentCombat(null, false); return; }
        BindSector();
        if (!IsInstanceValid(_enemy)) { _view.Visible = false; _view.PresentCombat(null, false); return; }

        if (Step == 0 && _enemy.CurrentState == BellBackLaborerState.Dead) Step = 1;
        if (Step == 1 && GroundedOn(_shelf)) Step = 2;
        if (Step == 2 && GroundedOn(_shelf) && _supply.OverlapsBody(_level.Player)) Step = 3;
        if (Step == 3 && GroundedOn(_floor) && _camp.OverlapsBody(_level.Player)) Step = 4;
        Present();
    }

    private void BindSector()
    {
        if (_sector == _level.CurrentSectorRoot && IsInstanceValid(_enemy)) return;
        _sector = _level.CurrentSectorRoot;
        _enemy = _sector.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer");
        _catch = _sector.GetNode<StaticBody2D>("Gameplay/Collision/EntryCatch");
        _shelf = _sector.GetNode<StaticBody2D>("Gameplay/Collision/SalvageFloor");
        _floor = _sector.GetNode<StaticBody2D>("Gameplay/Collision/BasinFloor");
        _supply = _sector.GetNode<PrologueSupplyCache>("Gameplay/OptionalPaths/SalvageCache/RecoverySupply");
        _camp = _sector.GetNode<Area2D>("Gameplay/Checkpoint");
    }

    private bool GroundedOn(StaticBody2D floor)
    {
        var player = _level.Player;
        if (!player.IsOnFloor()) return false;
        for (int i = 0; i < player.GetSlideCollisionCount(); i++)
        {
            var contact = player.GetSlideCollision(i);
            if (contact.GetCollider() == floor && contact.GetNormal().Dot(Vector2.Up) > .7f)
                return true;
        }
        return false;
    }

    private void Present()
    {
        bool en = _flow.Settings.Language == GameLanguage.English;
        string title, keys, body;
        Node2D target = null;
        Vector2 offset = Vector2.Zero;
        string label = string.Empty;
        if (Step == 0)
        {
            if (_enemy.CurrentState == BellBackLaborerState.Exposed)
            {
                title = en ? "Armor open — strike now" : "护甲已打开 · 现在反击";
                keys = en ? "J  ATTACK" : "J 攻击";
                body = en ? "Move close and strike before the armor closes.\nEach attack spends energy; release to recover."
                    : "靠近敌人，在护甲闭合前攻击。\n攻击消耗气力；松开进攻，留出恢复时间。";
            }
            else if (_enemy.CurrentState == BellBackLaborerState.Telegraph)
            {
                title = en ? "Sweep incoming — face the threat" : "横扫将至 · 面向威胁防御";
                keys = en ? "WASD + HOLD L  >  RELEASE" : "WASD 定向 + 按住 L  >  挡下后松开";
                body = en ? "The marked sweep shows the danger area.\nRelease your shield after the hit to cool it."
                    : "留意横扫预警范围，面朝敌人举盾。\n挡下后松开 L，让护盾散热。";
            }
            else if (_enemy.CurrentState == BellBackLaborerState.Attack)
            {
                title = en ? "Sweep active — keep clear" : "横扫中 · 保持防御或避让";
                keys = en ? "WASD FACE THREAT + HOLD L" : "WASD 面向敌人  +  按住 L";
                body = en ? "The marked sweep is dangerous now.\nRelease your shield only after the sweep ends."
                    : "标记范围内正在造成伤害。\n横扫结束后再松开 L，让护盾散热。";
            }
            else if (_enemy.CurrentState == BellBackLaborerState.Recover)
            {
                title = en ? "Recovering — armor still closed" : "敌人收招 · 护甲仍闭合";
                keys = en ? "WASD + K  >  OPEN ARMOR" : "WASD + K 抓敌人  >  先打开护甲";
                body = en ? "Recovery is not an exposed damage window.\nUse K to open armor, then move close and press J."
                    : "收招不等于破甲，直接攻击仍会被护甲挡住。\n先按 K 打开护甲，再靠近按 J 反击。";
            }
            else
            {
                title = en ? "Read the armor, then counter" : "看清护甲，再寻找反击机会";
                keys = en ? "J ATTACK  /  K CLAW  /  L SHIELD" : "J 攻击  /  K 机械爪  /  L 防御";
                body = en ? "A / D moves; WASD aims the claw and shield.\nK opens armor; move close and press J to strike."
                    : "A / D 移动，WASD 为机械爪和护盾定向。\n按 K 打开护甲，靠近后按 J 攻击。";
            }
        }
        else if (Step == 1)
        {
            title = en ? "Take the upper route" : "机械爪 · 探索上层路线";
            keys = en ? "WASD + K  /  A/D  /  SPACE" : "WASD + K  /  A/D  /  SPACE";
            body = en ? "Aim at the marked underside; a hit pulls automatically.\nAfter arrival, A/D can swing; SPACE leap-releases anytime."
                : "瞄准上方标记的平台底面；命中后自动牵引。\n抵达后可用 A/D 摆荡；SPACE 可随时跃出并松爪。";
            target = _catch; offset = new Vector2(0, 16);
            label = en ? "GRAB UNDERSIDE" : "可抓底面";
            if (_claw.CurrentTarget?.GetParent() == _catch &&
                _claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel
                    or MechanicalClawState.Swing or MechanicalClawState.Boost)
            {
                title = en ? "Connected — choose the landing" : "已抓牢 · 选好落点";
                keys = en ? "A/D AFTER ARRIVAL  /  SPACE ANYTIME" : "抵达后 A/D 摆荡  /  随时 SPACE 跃出";
                body = en ? "Auto-pull brings you closer.\nA/D optional swing after arrival.\nSPACE leap-releases anytime."
                    : "自动牵引靠近平台；抵达后可用 A/D 摆荡。\n牵引途中也能按 SPACE 借力跃出，不必等待抵达。";
                target = _shelf; offset = new Vector2(-150, -20);
                label = en ? "LAND HERE" : "安全落台";
            }
        }
        else if (Step == 2)
        {
            title = en ? "A safe ledge, a useful supply" : "安全落台 · 查看补给";
            keys = en ? "D  >  SUPPLY" : "D 向右靠近补给";
            body = en ? "Supplies restore missing health on contact.\nAt full health, the supply stays available."
                : "靠近补给可恢复缺失的生命。\n满血时不会消耗，留给需要的时候。";
            target = _supply; label = en ? "SUPPLY" : "补给";
        }
        else if (Step == 3)
        {
            title = _supply.IsSpent ? (en ? "Supply already recovered" : "补给已回收")
                : (en ? "Supply kept — available when needed" : "补给已保留 · 可按需取用");
            keys = en ? "A  >  RETURN TO CAMP" : "A 向左走  >  返回营地";
            body = en ? "Walk left off this ledge to the lower camp.\nThe landing below is safe; no extra jump needed."
                : "从高台左侧下落，返回下方营地。\n下方有安全地面，不需要额外跳跃。";
            target = _camp; label = en ? "CAMP" : "营地";
        }
        else
        {
            title = en ? "Exploration loop complete" : "已返回营地 · 本段探索完成";
            keys = en ? "ESC  MENU  /  FREE EXPLORATION" : "Esc 打开菜单  /  可继续自由探索";
            body = en ? "Combat, climb and supply route explored.\nThis is a development sample, not the full prologue."
                : "你已完成战斗、攀升、补给与归营回路。\n这是开发样片的体验段，并非整个序章通关。";
        }
        if (Step == 0) ApplyCombatReadiness(en, ref title, ref keys, ref body);
        _view.Present(Step, title, keys, body, Step == 4);
        _view.SetWorldFocus(target, offset, label);
        _view.PresentCombat(Step == 0 ? _enemy : null, en);
    }

    private void ApplyCombatReadiness(bool en, ref string title, ref string keys, ref string body)
    {
        bool heldShield = _input.Current.ShieldHeld;
        if (_enemy.CurrentState is BellBackLaborerState.Telegraph or BellBackLaborerState.Attack)
        {
            if (!_energy.IsOverheated && _energy.EnergyRatio > 0f) return;
            title = _energy.IsOverheated
                ? (en ? "Shield overheated — evade" : "护盾过热 · 先避开横扫")
                : (en ? "No shield energy — evade" : "气力耗尽 · 先避开横扫");
            keys = heldShield
                ? (en ? "RELEASE L  >  A / D TO EVADE" : "松开 L  >  A / D 离开横扫范围")
                : (en ? "A / D TO EVADE" : "A / D 离开横扫范围");
            body = en ? "The shield cannot protect you right now.\nMove out of the marked sweep while it recovers."
                : "护盾现在无法提供防御，不要原地硬挡。\n离开横扫标记范围，等待气力和热量恢复。";
            return;
        }
        if (_enemy.CurrentState == BellBackLaborerState.Exposed)
        {
            if (_modes.ActiveSlot != 1)
            {
                title = en ? "Armor open — empty slot" : "护甲已开 · 当前插槽为空";
                keys = en ? "1 EQUIP WEAPON  >  J" : "1 装备武器  >  J 攻击";
                body = en ? "Slots 2–4 do not contain weapons in this sample.\nSelect slot 1 before your next attack."
                    : "这份样板的 2–4 号插槽还没有武器。\n先选择 1 号武器，再靠近反击。";
            }
            else if (heldShield && _shield.IsActive)
            {
                title = en ? "Armor open — lower your shield" : "护甲已开 · 松盾再攻击";
                keys = en ? "RELEASE L  >  J" : "松开 L  >  J 攻击";
                body = en ? "An active held shield blocks your attack.\nRelease L, then press J again."
                    : "持续举盾时不能发动攻击。\n先松开 L，再重新按 J 反击。";
            }
            else if (!_modes.CanAffordAttack)
            {
                title = en ? "Low energy — wait to strike" : "护甲已开 · 等待气力恢复";
                keys = heldShield
                    ? (en ? "RELEASE L  >  RECOVER ENERGY" : "松开 L  >  等待气力恢复")
                    : (en ? "A / D KEEP CLEAR  >  RECOVER ENERGY" : "A / D 保持距离  >  等待气力恢复");
                body = en ? "You cannot afford another attack yet.\nMake space and recover; reopen armor if it closes."
                    : "当前气力不足以再次攻击。\n拉开距离恢复；护甲闭合后，可再用机械爪打开。";
            }
            return;
        }
        if (heldShield)
        {
            title = en ? "Release shield to use the claw" : "松开防御 · 再使用机械爪";
            keys = en ? "RELEASE L  >  WASD + K" : "松开 L  >  WASD + K 抓敌人";
            body = en ? "Holding L blocks the claw, even if overheated.\nRelease L, aim with WASD, then press K again."
                : "按着 L 会阻止机械爪，即使护盾已过热。\n松开 L，用 WASD 瞄准后重新按 K。";
        }
    }
}
