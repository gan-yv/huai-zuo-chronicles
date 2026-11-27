namespace Demo02.Combat
{
    public sealed class EnemyAttackCadence
    {
        private readonly int heavyEvery;
        private int attacksSinceHeavy;

        public EnemyAttackCadence(int heavyAttackEvery)
        {
            heavyEvery = System.Math.Max(1, heavyAttackEvery);
        }

        public bool NextIsHeavy =>
            attacksSinceHeavy >= heavyEvery - 1;

        public void CompleteAttack()
        {
            attacksSinceHeavy = NextIsHeavy
                ? 0
                : attacksSinceHeavy + 1;
        }

        public void Reset()
        {
            attacksSinceHeavy = 0;
        }
    }
}
