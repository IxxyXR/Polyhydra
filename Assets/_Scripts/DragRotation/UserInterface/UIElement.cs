/*************************************************
 * Author: Jeremy Fournier                       *
 *************************************************/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace DragRotation.UIElement
{
	public class UIElement : UIBehaviour
	{
		#region Variables
		public bool bool_OnMouseOver;
		public UnityEngine.Events.BooleanEvent event_OnMouseOver;

		public bool bool_OnMouseInput;
		public UnityEngine.Events.BooleanEvent event_OnMouseInput;
		#endregion

		#region Mouse Behaviours
		protected virtual void OnMouseEnter ()
		{
			if( bool_OnMouseOver == true )
			{
				event_OnMouseOver.Invoke ( true );
			}
		}

		protected virtual void OnMouseExit ()
		{
			if( bool_OnMouseOver == true )
			{
				event_OnMouseOver.Invoke ( false );
			}
		}

		protected virtual void OnMouseDown ()
		{
			if( bool_OnMouseInput == true )
			{
				event_OnMouseInput.Invoke ( true );
			}
		}

		protected virtual void OnMouseUp ()
		{
			if( bool_OnMouseInput == true )
			{
				event_OnMouseInput.Invoke ( false );
			}
		}
		#endregion
	}
}