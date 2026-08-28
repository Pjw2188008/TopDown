using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
	[SerializeField] private float moveSpeed = 5f;

	private void Update()
	{
		if (Keyboard.current == null)
		{
			return;
		}

		Vector2 moveDirection = Vector2.zero;

		if (Keyboard.current.wKey.isPressed)
		{
			moveDirection.y += 1f;
		}

		if (Keyboard.current.sKey.isPressed)
		{
			moveDirection.y -= 1f;
		}

		if (Keyboard.current.dKey.isPressed)
		{
			moveDirection.x += 1f;
		}

		if (Keyboard.current.aKey.isPressed)
		{
			moveDirection.x -= 1f;
		}

		transform.position += (Vector3)(moveDirection.normalized * moveSpeed * Time.deltaTime);
	}
}
