namespace Config_Assets
{
    using UnityEngine;

    public class Rotate : MonoBehaviour
    {
        public float Speed = 20;
        public Vector3 Dir;

        void Update()
        {
            transform.Rotate(Dir * Speed * Time.deltaTime);
        }
    }
}
