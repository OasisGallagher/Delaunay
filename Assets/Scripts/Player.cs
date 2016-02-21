using UnityEngine;

public class Player : MonoBehaviour
{
	public Vector3 Speed;

	void Start()
	{
	}

	void Update()
	{
		transform.position += Speed * Time.deltaTime;
	}
}
