using UnityEngine;

namespace ProfitSliders
{
    public static class ExtensionMethods
    {
        // LLM written
        public static Transform FindDescendant(this Transform transform, string name)
        {
            if (transform.name == name)
                return transform;

            foreach (Transform child in transform)
            {
                var result = child.FindDescendant(name);
                if (result)
                    return result;
            }

            return null;
        }
    }
}
