using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using CardBattleEngine;

namespace MyBenchmarks;
public class CardBattleEngineBenchmark
{

	//[Benchmark]
	//public void GameStateClone()
	//{
	//	Player p1 = new Player("Alice");
	//	Player p2 = new Player("Bob");
	//	IRNG rng = null;
	//	GameState gameState = new GameState(p1, p2, rng, []);

	//	gameState.Clone();
	//}

	//[Benchmark]
	//public void GameStateLightClone()
	//{
	//	Player p1 = new Player("Alice");
	//	Player p2 = new Player("Bob");
	//	IRNG rng = null;
	//	GameState gameState = new GameState(p1, p2, rng, []);

	//	gameState.LightClone();
	//}

	//[Benchmark]
	//public void EngineResolve()
	//{
	//	GameEngine engine = new GameEngine();
	//	Player p1 = new Player("Alice");
	//	Player p2 = new Player("Bob");
	//	IRNG rng = null;
	//	GameState gameState = new GameState(p1, p2, rng, []);

	//	MinionCard testCard = new MinionCard("Test", 1, 1, 1)
	//	{
	//		MinionTriggeredEffects = [new TriggeredEffect() {
	//			EffectTiming = EffectTiming.Post,
	//			EffectTrigger = EffectTrigger.Battlecry,
	//			GameActions = [new DamageAction()
	//			{
	//				Damage = (Value)1,
	//			}],
	//			AffectedEntitySelector = new ContextSelector(){
	//				IncludeTarget = true
	//			}
	//		}]
	//	};

	//	for(int i = 0; i < 6; i++)
	//	{
	//		p1.Hand.Add(testCard.Clone());
	//		p1.Board.Add(new Minion(testCard, p1));

	//		p2.Hand.Add(testCard.Clone());
	//		p2.Board.Add(new Minion(testCard, p2));
	//	}

	//	engine.Resolve(gameState,
	//		new ActionContext() 
	//		{
	//			SourcePlayer = p1,
	//			Source = p1.Board[0], 
	//			Target = p2.Board[0],
	//		}, new AttackAction());
	//}

	//[Benchmark]
	//public void EngineResolveSimpleSimulation()
	//{
	//	GameEngine engine = new GameEngine();
	//	engine.IsSimulation = true;
	//	Player p1 = new Player("Alice");
	//	Player p2 = new Player("Bob");
	//	IRNG rng = null;
	//	GameState gameState = new GameState(p1, p2, rng, []);

	//	MinionCard testCard = new MinionCard("Test", 1, 1, 1);

	//	for (int i = 0; i < 6; i++)
	//	{
	//		p1.Hand.Add(testCard.Clone());
	//		p1.Board.Add(new Minion(testCard, p1));

	//		p2.Hand.Add(testCard.Clone());
	//		p2.Board.Add(new Minion(testCard, p2));
	//	}

	//	engine.Resolve(gameState,
	//		new ActionContext()
	//		{
	//			SourcePlayer = p1,
	//			Source = p1.Board[0],
	//			Target = p2.Board[0],
	//		}, new AttackAction());
	//}

	//[Benchmark]
	//public void EngineResolveSimulation()
	//{
	//	GameEngine engine = new GameEngine();
	//	engine.IsSimulation = true;
	//	Player p1 = new Player("Alice");
	//	Player p2 = new Player("Bob");
	//	IRNG rng = null;
	//	GameState gameState = new GameState(p1, p2, rng, []);

	//	MinionCard testCard = new MinionCard("Test", 1, 1, 1)
	//	{
	//		MinionTriggeredEffects = [new TriggeredEffect() {
	//			EffectTiming = EffectTiming.Post,
	//			EffectTrigger = EffectTrigger.Battlecry,
	//			GameActions = [new DamageAction()
	//			{
	//				Damage = (Value)1,
	//			}],
	//			AffectedEntitySelector = new TargetOperationSelector() {
	//				Operations = [new SelectBoardEntitiesOperation(){
	//					Group = TargetGroup.Minions,
	//					Side = TeamRelationship.Enemy,
	//				}]
	//			}
	//		}]
	//	};

	//	for (int i = 0; i < 6; i++)
	//	{
	//		p1.Hand.Add(testCard.Clone());
	//		p1.Board.Add(new Minion(testCard, p1));

	//		p2.Hand.Add(testCard.Clone());
	//		p2.Board.Add(new Minion(testCard, p2));
	//	}

	//	engine.Resolve(gameState,
	//		new ActionContext()
	//		{
	//			SourcePlayer = p1,
	//			Source = p1.Board[0],
	//			Target = p2.Board[0],
	//		}, new AttackAction());
	//}

	[Params(1,2,3,4,5,6,7)]
	public int EntityCount;
	[Params(1, 2, 3, 4)]
	public int MaxDepth;

	private Player _player;
	private GameEngine _engine;
	private Random _random;

	[Benchmark]
	public void EngineIterateActions()
	{
		_random = new Random();
		_engine = new GameEngine();
		Player p1 = new Player("Alice");
		Player p2 = new Player("Bob");
		IRNG rng = null;
		GameState gameState = new GameState(p1, p2, rng, []);

		p1.Mana = 10;
		p2.Mana = 10;

		for (int i = 0; i < EntityCount; i++)
		{
			MinionCard testCard = new MinionCard("Test", 1, 1, 1)
			{
				HasCharge = true,
				HasDivineShield = true,
				MinionTriggeredEffects = [new TriggeredEffect() {
				EffectTiming = EffectTiming.Post,
				EffectTrigger = EffectTrigger.Battlecry,
				GameActions = [new DamageAction()
				{
					Damage = (Value)1,
				}],
				AffectedEntitySelector = new TargetOperationSelector() {
					Operations = [new SelectBoardEntitiesOperation(){
						Group = TargetGroup.Minions,
						Side = TeamRelationship.Enemy,
					}]
				}
			}]
			};
			testCard.Owner = p1;
			p1.Hand.Add(testCard);
			p1.Board.Add(new Minion(testCard, p1)
			{
				HasSummoningSickness = false,
			});
		}

		for (int i = 0; i < EntityCount; i++)
		{
			MinionCard testCard = new MinionCard("Test", 1, 1, 1)
			{
				MinionTriggeredEffects = [new TriggeredEffect() {
				EffectTiming = EffectTiming.Post,
				EffectTrigger = EffectTrigger.Battlecry,
				GameActions = [new DamageAction()
				{
					Damage = (Value)1,
				}],
				AffectedEntitySelector = new TargetOperationSelector() {
					Operations = [new SelectBoardEntitiesOperation(){
						Group = TargetGroup.Minions,
						Side = TeamRelationship.Enemy,
					}]
				}
			}]
			};
			testCard.Owner = p2;
			p2.Hand.Add(testCard);
			p2.Board.Add(new Minion(testCard, p2)
			{
				HasSummoningSickness = false,
			});
		}

		_player = p1;

		(float score, (IGameAction, ActionContext) bestAction) result = SearchTurn(gameState, 0);
	}

	private (float score, (IGameAction, ActionContext) bestAction) SearchTurn(GameState state, int depth)
	{
		if (depth >= MaxDepth)
		{
			var player = (Player)state.GetEntityById(_player.Id);
			return (Evaluate(state, player), (null, null));
		}

		var statePlayer = (CardBattleEngine.Player)state.GetEntityById(_player.Id);
		var actions = state.GetValidActions(statePlayer);

		float bestScore = float.NegativeInfinity;
		(IGameAction, ActionContext) bestAction = (null, null);

		int k = GetBranchCount(actions.Count(), depth);
		foreach (var action in actions.OrderBy(x => _random.Next()).Take(k))
		{
			var simState = state.LightClone();

			ActionContext clonedContext = CloneContextFor(simState, action.Item2);
			IGameAction clonedAction = CloneActionFor(simState, action.Item1);
			var simPlayer = (CardBattleEngine.Player)simState.GetEntityById(statePlayer.Id);

			var isValid = clonedAction.IsValid(simState, clonedContext, out string reason);
			if (!isValid)
			{
				continue;
			}

			_engine.Resolve(
				simState,
				clonedContext,
				clonedAction);

			float score = 0;

			if (action.Item1 is EndTurnAction)
			{
				score = Evaluate(simState, simPlayer);
			}
			else
			{
				var (childScore, _) = SearchTurn(simState, depth + 1);
				score = childScore;
			}

			if (score > bestScore)
			{
				bestScore = score;
				bestAction = action;
			}
		}

		return (bestScore, bestAction);
	}

	private int GetBranchCount(int totalActions, int depth)
	{
		if (depth == 0) return totalActions; // full width root

		// exponential decay
		double factor = Math.Pow(0.25, depth);
		int count = (int)Math.Ceiling(totalActions * factor);

		return Math.Max(1, count);
	}

	private IGameAction CloneActionFor(GameState simState, IGameAction action)
	{
		if (action is PlayCardAction playCardAction)
		{
			var newAction = new PlayCardAction();
			Guid id = playCardAction.Card.Id;
			newAction.Card = (CardBattleEngine.Card)simState.GetEntityById(id);
			return newAction;
		}

		return action;
	}

	private ActionContext CloneContextFor(GameState simState, ActionContext context)
	{
		return new ActionContext(context)
		{
			SourcePlayer = context.SourcePlayer == null ? null :
				(CardBattleEngine.Player)simState.GetEntityById(context.SourcePlayer.Id),

			SourceCard = context.SourceCard == null ? null :
				(CardBattleEngine.Card)simState.GetEntityById(context.SourceCard.Id),

			Source = context.Source == null ? null :
				simState.GetEntityById(context.Source.Id),

			Target = context.Target == null ? null :
				simState.GetEntityById(context.Target.Id),
		};
	}

	private float Evaluate(GameState state, CardBattleEngine.Player me)
	{
		var enemy = state.OpponentOf(me);

		float score = 0;

		// 1) Hero health (winning/losing matters most)
		//score += me.Health * 10;
		score -= enemy.Health * 10;

		// 2) Board presence (stats on board)
		score += me.Board.Sum(m => m.Attack * 1.5f + m.Health * 1.0f);
		score -= enemy.Board.Sum(m => m.Attack * 1.5f + m.Health * 1.0f);

		// 3) Immediate damage available THIS TURN (tempo)
		var myReadyAttack = me.Board.Where(m => m.CanAttack()).Sum(m => m.Attack);
		if (me.CanAttack()) { myReadyAttack += me.Attack; }
		var enemyReadyAttack = enemy.Board.Sum(m => m.Attack); //all enemy minion can attack next turn

		score += myReadyAttack * 2.0f;
		score -= enemyReadyAttack * 2.0f;

		// 4) Taunts (board locking is very valuable)
		var enemyTauntHealth = enemy.Board.Where(m => m.Taunt).Sum(m => m.Health);
		var myTauntHealth = me.Board.Where(m => m.Taunt).Sum(m => m.Health);

		score -= enemyTauntHealth * 1.5f;
		score += myTauntHealth * 1.5f;

		// 5) Card advantage
		score += me.Hand.Count * 1.5f;
		score -= enemy.Hand.Count * 1.5f;

		// 6) Lethal bonus (VERY IMPORTANT)
		if (myReadyAttack - enemyTauntHealth >= enemy.Health)
			score += 100_000;

		if (enemyReadyAttack - myTauntHealth >= me.Health)
			score -= 100_000;

		return score;
	}

}

public class Program
{
	public static void Main(string[] args)
	{
		var config = ManualConfig.Create(DefaultConfig.Instance)
			.AddJob(Job.Default
				.WithToolchain(InProcessNoEmitToolchain.Instance)
				.WithLaunchCount(1)
				.WithWarmupCount(1)
				.WithIterationCount(3)
			);
		var summary = BenchmarkRunner.Run<CardBattleEngineBenchmark>(config);
	}
}