using UnityEngine;

namespace Delaunay
{
	public class PlayerComponent : MonoBehaviour
	{
		/// <summary>
		/// �ƶ��ٶ�.
		/// </summary>
		public float Speed = 8f;

		/// <summary>
		/// �뾶.
		/// </summary>
		public float Radius
		{
			get { return radius; }
			set { if (!Mathf.Approximately(radius, value)) { UpdateRadius(value); } }
		}

		float radius = 0.5f;

		void Start()
		{
			UpdateRadius(radius);
		}

		void UpdateRadius(float r)
		{
			radius = r;
			transform.localScale = new Vector3(radius * 2f, 1, radius * 2f);
		}
	}
}
