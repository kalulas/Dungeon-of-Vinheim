namespace Invector
{
    public class vAnimatorSetInt : vAnimatorSetValue<int>
    {
        [vHelpBox("Random Value between Default Value and Max Value")]
        public bool randomEnter;
        [vHideInInspector("randomEnter")]
        public int maxEnterValue;
        public bool randomExit;
        [vHideInInspector("randomExit")]
        public int maxExitValue;

        protected override int GetEnterValue()
        {
            return randomEnter ? UnityEngine.Random.Range(base.GetEnterValue(), maxEnterValue) : base.GetEnterValue();
        }
        protected override int GetExitValue()
        {
            return randomExit ? UnityEngine.Random.Range(base.GetExitValue(), maxExitValue) : base.GetExitValue();
        }
    }
}