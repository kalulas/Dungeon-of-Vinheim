using UnityEngine;
using System.Collections;

namespace Invector
{
    using vEventSystems;
    public class vBreakableObject : MonoBehaviour, vIDamageReceiver
    {
        public Transform brokenObject;
        [Header("Break Object Settings")]
        [Tooltip("Break objet  OnTrigger with Player rolling")]
        public bool breakOnPlayerRoll = true;
        [Tooltip("Break objet  OnCollision with other object")]
        public bool breakOnCollision = true;
        [Tooltip("Rigidbody velocity to break OnCollision whit other object")]
        public float maxVelocityToBreak = 5f;
        public UnityEngine.Events.UnityEvent OnBroken;
        [SerializeField] protected OnReceiveDamage _onReceiveDamage = new OnReceiveDamage();
        public OnReceiveDamage onReceiveDamage { get { return _onReceiveDamage; } protected set { _onReceiveDamage = value; } }
        private bool isBroken;
        private Collider _collider;
        private Rigidbody _rigidBody;

        void Start()
        {
            _collider = GetComponent<Collider>();
            _rigidBody = GetComponent<Rigidbody>();
        }

        public void TakeDamage(vDamage damage)
        {
            if (!isBroken)
            {
                isBroken = true;
                StartCoroutine(BreakObjet());
            }
        }

        IEnumerator BreakObjet()
        {
            if (_rigidBody) Destroy(_rigidBody);
            if (_collider) Destroy(_collider);
            yield return new WaitForEndOfFrame();
            brokenObject.transform.parent = null;
            brokenObject.gameObject.SetActive(true);
            OnBroken.Invoke();
            Destroy(gameObject);
        }

#if INVECTOR_BASIC
        void OnTriggerStay(Collider other)
        {
            if (breakOnPlayerRoll && other.gameObject.CompareTag("Player"))
            {
                var thirPerson = other.gameObject.GetComponent<Invector.vCharacterController.vThirdPersonController>();
                if (thirPerson && thirPerson.isRolling && !isBroken)
                {
                    isBroken = true;
                    StartCoroutine(BreakObjet());
                }
            }
        }
#endif

        void OnCollisionEnter(Collision other)
        {
            if (breakOnCollision && _rigidBody && _rigidBody.velocity.magnitude > 5f && !isBroken)
            {
                isBroken = true;
                StartCoroutine(BreakObjet());
            }
        }
    }
}