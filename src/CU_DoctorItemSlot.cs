using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CUCompanion;

public class CU_DoctorItemSlot : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public Item item;

	public MedicalPartnerAssistant assistant;

	public Image slotBg;

	public Outline slotOutline;

	public Image iconImg;

	public TextMeshProUGUI nameTxt;

	public TextMeshProUGUI condTxt;

	public static Item DraggingItem { get; private set; }

	public void OnPointerClick(PointerEventData eventData)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)eventData.button != 0)
		{
			return;
		}
		if (eventData.clickCount >= 2)
		{
			if ((Object)(object)assistant != (Object)null && (Object)(object)item != (Object)null)
			{
				assistant.ApplyItemToCurrentSelection(item);
			}
		}
		else if ((Object)(object)assistant != (Object)null)
		{
			assistant.SelectPackSlot(this);
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)item == (Object)null))
		{
			DraggingItem = item;
			if ((Object)(object)assistant != (Object)null)
			{
				assistant.StartItemDrag(item, eventData.position);
			}
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)assistant != (Object)null)
		{
			assistant.UpdateItemDrag(eventData.position);
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		DraggingItem = null;
		if ((Object)(object)assistant != (Object)null)
		{
			assistant.EndItemDrag();
		}
	}
}
