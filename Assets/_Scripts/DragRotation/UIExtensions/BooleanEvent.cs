/*************************************************
 * Author: Jeremy Fournier                       *
 *************************************************/
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace UnityEngine.Events
{
	/// <summary>
	/// Extends UnityEvent to create an event that allows for Dynamic boolean functions
	/// </summary>
	[System.Serializable]
	public class BooleanEvent : UnityEngine.Events.UnityEvent<bool>
	{
	}
}