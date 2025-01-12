using System;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Runtime;

public readonly record struct SkillPanelConfig(IAssetRef<Skill>? skill);

public class GlobalDiscoConfig
{
	private SkillPanelConfig[] realPanels = {
		new(Skills.Logic), new(Skills.Encyclopedia), new(Skills.Rhetoric), new(Skills.Drama), new(Skills.Conceptualization), new(Skills.VisualCalculus),
		new(Skills.Volition), new(Skills.InlandEmpire), new(Skills.Empathy), new(Skills.Authority), new(Skills.EspritDeCorps), new(Skills.Suggestion),
		new(Skills.Endurance), new(Skills.PainThreshold), new(Skills.PhysicalInstrument), new(Skills.Electrochemistry), new(Skills.Shivers), new(Skills.HalfLight),
		new(Skills.HandEyeCoordination), new(Skills.Perception), new(Skills.ReactionSpeed), new(Skills.SavoirFaire), new(Skills.Interfacing), new(Skills.Composure)
	};

	public SkillPanelConfig[] panels
	{
		get => realPanels;
		set
		{
			if (value.Length == 24) realPanels = value;
			else throw new InvalidOperationException("expected an array of skill panels with exactly 24 entries.");
		}
	}
}
