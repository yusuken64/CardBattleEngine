using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardBattleEngine;

public class AddStatModifierAction : GameActionBase
{
	public int AttackChange { get; set; }
	public int HealthChange { get; set; }
	public EffectDuration Duration = EffectDuration.Permanent;
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		// Valid if target is still alive / on board
		return actionContext.Target != null && actionContext.Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state	, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext))
			return [];

		if (actionContext.Target is Minion minion)
		{
			minion.AddModifier(new StatModifier
			{
				AttackChange = AttackChange,
				HealthChange = HealthChange,
				Duration = Duration,
				SourcePlayer = actionContext.SourcePlayer
			});
		}


		var sideEffects = new List<(IGameAction, ActionContext)>();

		// Check for death triggers
		if(actionContext.Target.Health <= 0)
		{
			sideEffects.Add((new DeathAction(), new ActionContext()
			{
				SourcePlayer = actionContext.Target.Owner,
				Source = actionContext.Target,
			}));
		}

		return sideEffects;
	}

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		AttackChange = ReadInt(actionParam, nameof(AttackChange));
		HealthChange = ReadInt(actionParam, nameof(HealthChange));
	}

	private static int ReadInt(Dictionary<string, object> dict, string key)
	{
		if (!dict.TryGetValue(key, out var raw) || raw == null)
			return 0;

		switch (raw)
		{
			case int i:
				return i;

			case long l:
				return (int)l;

			case double d:
				return (int)d;

			case JValue jv when jv.Type == JTokenType.Integer:
				return jv.Value<int>();

			case JValue jv when jv.Type == JTokenType.Float:
				return (int)jv.Value<double>();

			case JToken jt:
				return jt.Value<int>(); // safe for numeric tokens

			case string s when int.TryParse(s, out int parsed):
				return parsed;

			default:
				return 0;
		}
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(AttackChange), AttackChange },
			{ nameof(HealthChange), HealthChange }
		};
	}
}

public class RemoveModifierAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		if (context.Target is not Minion minion)
		{
			return false;
		}

		return minion.HasModifier(context.Modifier);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is not Minion minion)
		{
			return [];
		}

		minion.RemoveModifier(context.Modifier);

		return [];
	}
}

public class StatModifier
{
	public int AttackChange;
	public int HealthChange;
	public EffectDuration Duration;
	public Player SourcePlayer;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectDuration
{
	Permanent,
	UntilEndOfTurn,
	Aura,
}