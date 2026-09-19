using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;

public class ComboStateMachineTests
{
    private ComboStateMachine combo;
    private AttackStep[] pasos;
    private AttackStep cargado;

    [SetUp]
    public void Setup()
    {
        pasos = new AttackStep[]
        {
            new AttackStep { Damage = 10f, ActiveTime = 0.3f, WindupTime = 0.2f, ComboWindowTime = 0.25f },
            new AttackStep { Damage = 12f, ActiveTime = 0.4f, WindupTime = 0.25f, ComboWindowTime = 0.3f },
            new AttackStep { Damage = 15f, ActiveTime = 0.5f, WindupTime = 0.3f, ComboWindowTime = 0.35f }
        };

        cargado = new AttackStep { Damage = 25f, ActiveTime = 0.6f, WindupTime = 0.3f, ComboWindowTime = 0.3f };

        combo = new ComboStateMachine(pasos, cargado, 0.15f, 1.2f);
    }

    [Test]
    public void Tres_taps_producen_tres_golpes_ligeros_en_Ayla()
    {
        var input = new InputSnapshot { AttackPressed = true };
        combo.Tick(input, 0.2f);
        combo.Tick(input, 0.2f);
        combo.Tick(input, 0.2f);
        Assert.AreEqual(ComboPhase.Swing, combo.Fase);
    }

    [Test]
    public void Dos_taps_producen_dos_golpes_ligeros_en_Zendre()
    {
        var input = new InputSnapshot { AttackPressed = true };
        combo.Tick(input, 0.25f);
        combo.Tick(input, 0.25f);
        Assert.AreEqual(ComboPhase.Swing, combo.Fase);
    }

    [Test]
    public void Hold_desde_neutral_produce_cargado_puro_sin_ligero_previo()
    {
        var input = new InputSnapshot { AttackHeld = true, AttackPressed = true};
        combo.Tick(input, 0.3f);
        combo.Tick(input, 1.2f);
        Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
    }

    [Test]
    public void Tap_tap_hold_produce_dos_ligeros_y_un_cargado()
    {
        var input = new InputSnapshot { AttackHeld = true, AttackPressed = true };
        combo.Tick(input, 0.2f);
        combo.Tick(input, 0.2f);
        input.AttackHeld = true;
        combo.Tick(input, 1.2f);
        Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
    }

    [Test]
    public void El_cargado_siempre_termina_el_combo()
    {
        var input = new InputSnapshot { AttackHeld = true, AttackPressed = true };
        combo.Tick(input, 1.2f);
        combo.Tick(input, 0.6f);
        Assert.AreEqual(ComboPhase.Idle, combo.Fase);
    }

    [Test]
    public void Al_llegar_al_ultimo_paso_el_combo_termina()
    {
        var input = new InputSnapshot { AttackPressed = true };
        for (int i = 0; i < pasos.Length; i++)
            combo.Tick(input, 0.3f);
        Assert.AreEqual(ComboPhase.Idle, combo.Fase);
    }

    [Test]
    public void La_ventana_de_combo_expira_y_reinicia_el_indice()
    {
        var input = new InputSnapshot { AttackPressed = true };
        combo.Tick(input, 0.3f);
        combo.Tick(new InputSnapshot(), 1.0f);
        Assert.AreEqual(ComboPhase.Idle, combo.Fase);
    }

    [Test]
    public void Un_press_en_frames_activos_encadena_el_siguiente_golpe()
    {
        var input = new InputSnapshot { AttackPressed = true };
        combo.Tick(input, 0.2f);
        combo.Tick(input, 0.1f);
        Assert.AreEqual(ComboPhase.ComboWindow, combo.Fase);
    }

    [Test]
    public void La_carga_se_libera_sola_al_llegar_al_maximo()
    {
        var input = new InputSnapshot { AttackHeld = true, AttackPressed = true };
        combo.Tick(input, 1.3f);
        Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
    }

    [Test]
    public void El_ChargeRatio_va_de_cero_a_uno()
    {
        var input = new InputSnapshot { AttackHeld = true };
        combo.Tick(input, 0.6f);
        combo.Tick(input, 1.2f);
        Assert.GreaterOrEqual(combo.Fase, ComboPhase.ChargedSwing);
    }
}
