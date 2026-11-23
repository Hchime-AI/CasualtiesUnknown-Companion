using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;

namespace CUCompanion;

public class CU_LimbClickHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IDropHandler
{
	public WoundViewLimb limbView;

	public void OnPointerClick(PointerEventData eventData)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)eventData.button == 0)
		{
			SelectThisLimb();
		}
	}

	public void SelectThisLimb()
	{
		if ((Object)(object)limbView == (Object)null || (Object)(object)limbView.woundview == (Object)null)
		{
			return;
		}
		Body body = limbView.woundview.body;
		if ((Object)(object)body == (Object)null || body.limbs == null)
		{
			return;
		}
		int limb = limbView.limb;
		if (limb < 0 || limb >= body.limbs.Length)
		{
			return;
		}
		Limb val = body.limbs[limb];
		if (!((Object)(object)val == (Object)null) && !val.dismembered)
		{
			limbView.woundview.limbLookingAt = limb;
			if ((Object)(object)PlayerCamera.main != (Object)null)
			{
				PlayerCamera.main.selectedLimb = val;
			}
			if (limbView.woundview.limbImageFlash != null && limb < limbView.woundview.limbImageFlash.Length)
			{
				limbView.woundview.limbImageFlash[limb] = 1.6f;
			}
			MedicalPartnerAssistant component = ((Component)limbView.woundview).GetComponent<MedicalPartnerAssistant>();
			if ((Object)(object)component != (Object)null)
			{
				component.OnLimbSelectedByClick(val);
			}
		}
	}

	public void OnDrop(PointerEventData eventData)
	{
		if (!((Object)(object)CU_DoctorItemSlot.DraggingItem != (Object)null) || !((Object)(object)limbView != (Object)null) || !((Object)(object)limbView.woundview != (Object)null))
		{
			return;
		}
		Body body = limbView.woundview.body;
		if (!((Object)(object)body != (Object)null) || body.limbs == null || limbView.limb < 0 || limbView.limb >= body.limbs.Length)
		{
			return;
		}
		Limb val = body.limbs[limbView.limb];
		if ((Object)(object)val != (Object)null && !val.dismembered)
		{
			SelectThisLimb();
			MedicalPartnerAssistant component = ((Component)limbView.woundview).GetComponent<MedicalPartnerAssistant>();
			if ((Object)(object)component != (Object)null)
			{
				component.ApplyItemDirect(CU_DoctorItemSlot.DraggingItem, val);
			}
		}
	}
}
