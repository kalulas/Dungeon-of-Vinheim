using UnityEngine;

namespace Invector
{
    [System.Serializable]
    public class OnDead : UnityEngine.Events.UnityEvent<GameObject> { }
    public interface vIHealthController : vIDamageReceiver
    {
        OnDead onDead { get; }
        float currentHealth { get; }
        int MaxHealth { get; }
        bool isDead { get; set; }
        void ChangeHealth(int value);
        void ChangeMaxHealth(int value);
    }
    public static class vHealthControllerHelper
    {
        static vIHealthController GetHealthController(this GameObject gameObject)
        {
            return gameObject.GetComponent<vIHealthController>();
        }

        /// <summary>
        /// Check if GameObject Has a vIHealthController 
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static bool HasHealth(this GameObject gameObject)
        {
            return gameObject.GetHealthController() != null;
        }

        /// <summary>
        /// Check if GameObject is dead
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>return true if GameObject does not has a vIHealthController or currentHealth is less or equals zero </returns>
        public static bool IsDead(this GameObject gameObject)
        {
            var health = gameObject.GetHealthController();
            return health == null || health.isDead;
        }
    }
}

