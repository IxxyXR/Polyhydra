/*************************************************
 * Author: Jeremy Fournier                       *
 *************************************************/
using UnityEngine;

namespace DragRotation
{
	[RequireComponent ( typeof ( DragRotation.UIElement.UIElement ) )]
	[RequireComponent ( typeof ( UnityEngine.Rigidbody ) )]
	public class Rotator : MonoBehaviour
	{
		#region Variables
		/// <summary>
		/// The Rigidbody used for it's Angular Velocity function
		/// </summary>
		public new UnityEngine.Rigidbody rigidbody;

		/// <summary>
		/// The input state of the object, when true this item is being dragged
		/// </summary>
		private bool bool_Dragging = false;

		/// <summary>
		/// The transform's rotation at the start of the drag
		/// </summary>
		private Quaternion quaternion_Original;

		/// <summary>
		/// The offset from the transform's up direction to apply to our rotation to focus the target area under the cursor
		/// </summary>
		private Quaternion quaternion_Offset;

		/// <summary>
		/// References the current direction we're dragging
		/// </summary>
		private Vector3 vector3_Direction;

		/// <summary>
		/// References the Direction we were dragging in during the previous frame
		/// </summary>
		private Vector3 vector3_PreviousDirection;
		#endregion

		void Start()
		{
			rigidbody.angularVelocity = new Vector3(0.1f, 0.2f, 0.3f);
		}

		#region Update
		/// <summary>
		/// Calls our Rotate function every frame that we're dragging the object.
		/// </summary>
		/// <remarks>
		/// This could have been better done via coroutine to avoid unnecessary Update calls when the object isn't being interacted with.
		/// </remarks>
		private void Update ()
		{
			if( bool_Dragging == true )
			{
				Rotate ();
			}
		}
		#endregion

		#region Manual
		/// <summary>
		/// Used to signal that this transform when this transform is being dragged or not
		/// </summary>
		/// <param name="state">The state of our drag</param>
		public void OnDrag ( bool state )
		{
			if( state == true )
			{
				// Initialize our rotation values
				initialize_Rotation ();

				// Flag draggins as true to begin to Rotate function
				bool_Dragging = true;
			}
			else
			{
				// Set the Angular Velocity when Draggin is released
				set_AngularVelocity ();

				// Set dragging to false
				bool_Dragging = false;
			}
		}
		#endregion

		#region Rotation
		/// <summary>
		/// Sets the initial values for the Rotate calls.
		/// </summary>
		private void initialize_Rotation ()
		{
			RaycastHit raycastHit;
			Ray ray = Camera.main.ScreenPointToRay ( UnityEngine.Input.mousePosition );

			if( Physics.Raycast ( ray, out raycastHit, Mathf.Infinity, ( 1 << 8 ) ) == true )
			{
				/// Since we want to rotate with a focus on the point of input and not the pivot point of the object we're dragging we must offset our rotation to facilitate this

				// Store this transform's current rotation
				quaternion_Original = transform.rotation;

				// Calculate the direction from the objects position towards the input point
				vector3_Direction = raycastHit.point - transform.position;

				// Create a quaternion in the direction of our targetted point
				Quaternion quaternion_LookRotation = Quaternion.LookRotation ( vector3_Direction );

				// Determine value with which to offset our rotation from
				quaternion_Offset = Quaternion.Inverse ( quaternion_LookRotation ) * quaternion_Original;
			}
		}

		/// <summary>
		/// Called every frame while bool_Draggin is true to update the rotation of the transform.
		/// </summary>
		private void Rotate ()
		{
			RaycastHit raycastHit;
			Ray ray = Camera.main.ScreenPointToRay ( UnityEngine.Input.mousePosition );

			if( Physics.Raycast ( ray, out raycastHit, Mathf.Infinity, ( 1 << 8 ) ) == true )
			{
				// Store the previous direction for velocity calculations on release
				vector3_PreviousDirection = vector3_Direction;

				// Calculate the direction from the objects position towards the input point
				vector3_Direction = raycastHit.point - transform.position;

				// Create a quaternion in the direction of our targetted point
				Quaternion quaternion_LookRotation = Quaternion.LookRotation ( vector3_Direction );

				// Apply our original offset with the calculated LookRotation then apply that and use it to set our rotation
				transform.rotation = ( quaternion_LookRotation * quaternion_Offset );
			}
		}
		#endregion

		#region AngularVelocity
		/// <summary>
		/// Calculate and apply the AngularVelocity to the attached Rigidbody
		/// </summary>
		private void set_AngularVelocity ()
		{
			/// I made an attempt to perform this without a rigidbody, but kept running into gimble lock situations, so decided to let Unity do the work instead
			if( rigidbody != null )
			{
				// Calculates the Linear Velocity from the previous and current direction values
				Vector3 vector3_LinearVelocity = ( vector3_Direction - vector3_PreviousDirection ) / Time.deltaTime;

				// Calculate the angular velocity (r*p/|r|^2) and apply it to the attached Rigibody
				rigidbody.angularVelocity = ( Vector3.Cross ( vector3_Direction, vector3_LinearVelocity ) / vector3_Direction.sqrMagnitude );
			}
		}
		#endregion
	}
}