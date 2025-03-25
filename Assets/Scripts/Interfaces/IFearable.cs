using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public interface IFearable
    {
        void ApplyFear(float duration);
    }
} 