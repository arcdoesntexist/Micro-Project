namespace CPS.KitchenCatch
{
    public enum DifficultyId { Easy, Normal, Hard }
    public enum SessionState { Ready, Playing, Triumph, Dying, Results }
    public enum ObjectKind { Food, Trash }
    public enum ObjectOutcome { Caught, Missed }
    public enum ComboRank { D, C, B, A, S, SS, SSS }
    public enum EndReason { Triumph, HpDepleted }
}
