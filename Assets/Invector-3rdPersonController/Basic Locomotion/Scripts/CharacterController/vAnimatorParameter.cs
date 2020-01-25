using UnityEngine;
namespace Invector
{
    public class vAnimatorParameter
    {
        readonly AnimatorControllerParameter _parameter;

        public static implicit operator int(vAnimatorParameter a)
        {
            if (a.isValid) return a._parameter.nameHash;
            else
                return -1;
        }

        public readonly bool isValid;

        public vAnimatorParameter(Animator animator, string parameter)
        {
            if (animator && animator.ContainsParam(parameter))
            {
                _parameter = animator.GetValidParameter(parameter);
                this.isValid = true;
            }

            else this.isValid = false;
        }
    }
    public static class vAnimatorParameterHelper
    {
        public static AnimatorControllerParameter GetValidParameter(this Animator _Anim, string _ParamName)
        {
            foreach (AnimatorControllerParameter param in _Anim.parameters)
            {
                if (param.name == _ParamName) return param;
            }
            return null;
        }

        public static bool ContainsParam(this Animator _Anim, string _ParamName)
        {
            foreach (AnimatorControllerParameter param in _Anim.parameters)
            {
                if (param.name == _ParamName) return true;
            }
            return false;
        }

        public static bool HasParameterOfType(this Animator self, string name, AnimatorControllerParameterType type)
        {
            if (null == self)
            {
                return false;
            }

            var parameters = self.parameters;
            foreach (var currParam in parameters)
            {
                if (currParam.type == type && currParam.name == name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}