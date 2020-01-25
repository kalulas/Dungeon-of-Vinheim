using UnityEngine;
namespace Invector.vCharacterController.vActions
{
    public interface IActionController
    {
        bool enabled { get; set; }
        GameObject gameObject { get; }
        Transform transform { get; }
        string name { get; }
        System.Type GetType();
    }

    public interface IActionReceiver : IActionController
    {
        void OnReceiveAction(vTriggerGenericAction genericAction);
    }

    public interface IActionEnterListener : IActionController
    {
        void OnActionEnter(Collider actionCollider);
    }

    public interface IActionExitListener : IActionController
    {
        void OnActionExit(Collider actionCollider);
    }

    public interface IActionStayListener : IActionController
    {
        void OnActionStay(Collider actionCollider);

    }
    public interface IActionListener : IActionEnterListener, IActionExitListener, IActionStayListener
    {
        bool useActionEnter { get; }
        bool useActionExit { get; }
        bool useActionStay { get; }
        bool doingAction { get; }

    }
    public abstract class vActionListener : vMonoBehaviour, IActionListener
    {
        [HideInInspector]
        public bool actionEnter;
        [HideInInspector]
        public bool actionStay;
        [HideInInspector]
        public bool actionExit;

        public bool useActionEnter { get { return actionEnter; } }
        public bool useActionExit { get { return actionExit; } }
        public bool useActionStay { get { return actionStay; } }
        public bool doingAction { get { return _doingAction; } protected set { _doingAction = value; } }
        [HideInInspector]
        public bool _doingAction;
        [vEditorToolbar("Events", order = 10)]
        public vOnActionHandle OnDoAction = new vOnActionHandle();

        protected virtual void Start()
        {
            var actionReceivers = GetComponents<IActionReceiver>();
            for (int i = 0; i < actionReceivers.Length; i++) OnDoAction.AddListener(actionReceivers[i].OnReceiveAction);
        }

        public virtual void OnActionEnter(Collider other)
        {

        }

        public virtual void OnActionStay(Collider other)
        {

        }

        public virtual void OnActionExit(Collider other)
        {

        }


    }
    [System.Serializable]
    public class vOnActionHandle : UnityEngine.Events.UnityEvent<vTriggerGenericAction>
    {

    }
}