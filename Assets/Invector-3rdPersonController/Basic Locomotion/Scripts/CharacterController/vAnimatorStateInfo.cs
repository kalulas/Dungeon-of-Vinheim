using System.Collections.Generic;
using UnityEngine;

namespace Invector.vEventSystems
{
    public class vAnimatorStateInfos
    {
        public Animator animator;
        public vAnimatorStateInfos(Animator animator)
        {
            this.animator = animator;           
        }

        public void RegisterListener()
        {           
            var bhv = animator.GetBehaviours<vAnimatorTagBase>();
            for (int i = 0; i < bhv.Length; i++)
            {
                bhv[i].RemoveStateInfoListener(this);
                bhv[i].AddStateInfoListener(this);
            }
        }

        public void RemoveListener()
        {
            statesRunning.Clear();
            if (animator)
            {
                var bhv = animator.GetBehaviours<vAnimatorTagBase>();
                for (int i = 0; i < bhv.Length; i++)
                {
                    bhv[i].RemoveStateInfoListener(this);
                }
            }
        }

        Dictionary<string, List<int>> statesRunning = new Dictionary<string, List<int>>();
        public int currentlayer;
      
        internal void AddStateInfo(string tag, int info)
        {
            if (!statesRunning.ContainsKey(tag)) statesRunning.Add(tag, new List<int>() { info });
            else statesRunning[tag].Add(info);
            currentlayer = info;
        }

        internal void RemoveStateInfo(string tag, int info)
        {
            if (statesRunning.ContainsKey(tag) && statesRunning[tag].Exists(_info => _info.Equals(info)))
            {
                var inforef = statesRunning[tag].Find(_info => _info.Equals(info));
                statesRunning[tag].Remove(inforef);
                if (statesRunning[tag].Count == 0)
                    statesRunning.Remove(tag);
            }
            if (currentlayer == info) currentlayer = -1;
        }

        /// <summary>
        /// Check If StateInfo list contains tag
        /// </summary>
        /// <param name="tag">tag to check</param>
        /// <returns></returns>
        public bool HasTag(string tag)
        {
            return statesRunning.ContainsKey(tag);
        }

        /// <summary>
        /// Check if All tags in in StateInfo List
        /// </summary>
        /// <param name="tags">tags to check</param>
        /// <returns></returns>
        public bool HasAllTags(params string[] tags)
        {
            var has = tags.Length > 0 ? true : false;
            for (int i = 0; i < tags.Length; i++)
            {
                if (!HasTag(tags[i]))
                {
                    has = false;
                    break;
                }
            }
            return has;
        }

        /// <summary>
        /// Check if StateInfo List Contains any tag
        /// </summary>
        /// <param name="tags">tags to check</param>
        /// <returns></returns>
        public bool HasAnyTag(params string[] tags)
        {
            var has = false;
            for (int i = 0; i < tags.Length; i++)
            {
                if (HasTag(tags[i]))
                {
                    has = true;
                    break;
                }
            }
            return has;
        }

        public AnimatorStateInfo? GetCurrentAnimatorStateUsingTag(string tag)
        {
            if (currentlayer!=-1 && HasTag(tag) && statesRunning[tag].Exists(_inf =>_inf.Equals(currentlayer)))
            { 
                return animator.GetCurrentAnimatorStateInfo(currentlayer);
            }
            else return null;
        }
    }

}
