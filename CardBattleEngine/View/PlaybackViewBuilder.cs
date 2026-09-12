namespace CardBattleEngine.View;

public static partial class PlayerViewBuilder
{
    // Called synchronously at the playback callback. All lists and entity values are copied now.
    public static PlaybackEventView BuildPlayback(GameState state, Player viewer,
        IGameAction action, ActionContext context, long sequence, bool includePrivate = false)
    {
        bool secret = context.SourceCard is SpellCard spell && spell.Owner != viewer &&
            spell.SpellCastEffects.Any(e => e.GameActions.Any(a => a is SecretAction)) &&
            (action is PlayCardAction || action is CastSpellAction || action is SecretAction ||
             spell.Owner.Secrets.Any(s => s.SourceCard == spell));
        bool revealPlayed = !secret && (action is PlayCardAction || action is CastSpellAction);
        bool publicSourceCard = !secret && context.SourceCard?.Owner != null &&
            !context.SourceCard.Owner.Hand.Contains(context.SourceCard) &&
            !context.SourceCard.Owner.Deck.Contains(context.SourceCard);

        PlaybackEntityView Entity(IGameEntity entity, bool revealCard = false)
        {
            if (entity == null) return null;
            if (entity is Player player) return new PlaybackEntityView { Id = player.Id, OwnerId = player.Id };
            if (entity is Minion minion)
                return new PlaybackEntityView { Id = minion.Id, OwnerId = minion.Owner.Id, Minion = BuildMinionView(minion) };
            if (entity is Card card && (includePrivate || revealCard || card.Owner == viewer && card.Owner.Hand.Contains(card)))
                return new PlaybackEntityView { Id = card.Id, OwnerId = card.Owner.Id, Card = BuildCardView(card) };
            return null;
        }

        var result = new PlaybackEventView
        {
            Sequence = sequence,
            ActionType = secret ? "SecretPlayed" : action.GetType().Name,
            PlayerId = context.SourcePlayer?.Id ?? context.Source?.Owner?.Id ?? viewer.Id,
            Source = secret ? null : Entity(context.Source, revealPlayed || (publicSourceCard && context.Source == context.SourceCard)),
            Target = secret ? null : Entity(context.Target),
            SourceCard = secret ? null : Entity(context.SourceCard, revealPlayed || publicSourceCard),
            SummonedMinion = Entity(context.SummonedMinion),
            CardGained = Entity(context.CardGained),
            TriggerSource = secret ? null : Entity((action as TriggerEffectAction)?.TriggerSource?.Entity),
            DamageDealt = context.DamageDealt,
            HealedAmount = context.HealedAmount,
            ArmorGained = context.ArmorGained,
            ManaSpent = (action as SpendManaAction)?.Amount ?? 0,
            PlayIndex = context.PlayIndex,
            CardsLeftInDeck = context.CardsLeftInDeck,
            IsAttack = context.IsAttack,
            HiddenCardPlayed = secret && action is PlayCardAction,
            // Effects on private draws/choices also must not disclose the hidden card identity.
            PresentationEffectId = secret || (context.SourceCard != null && Entity(context.SourceCard, revealPlayed || publicSourceCard) == null)
                ? null : action.PresentationEffectId,
            After = Build(state, viewer, canonicalLegalActions: new List<(IGameAction, ActionContext)>()),
        };
        result.After.PendingChoice = null;
        result.After.PlaybackSequence = sequence;
        if (!secret)
        {
            foreach (var item in context.AffectedEntities)
            {
                var target = Entity(item.Item1);
                if (target != null) result.AffectedEntities.Add(new PlaybackAmountView { Target = target, Amount = item.Item2 });
            }
            foreach (var item in context.ResolvedStatusChanges)
            {
                var target = Entity(item.Target);
                if (target != null) result.StatusChanges.Add(new PlaybackStatusView { Target = target, Status = item.Status, Gained = item.Gained });
            }
        }
        return result;
    }
}
