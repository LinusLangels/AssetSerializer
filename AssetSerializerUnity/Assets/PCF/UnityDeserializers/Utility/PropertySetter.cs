using System;

namespace PCFFileFormat
{
	public class PropertySetter
	{
		private string fieldName;
		private Action<System.Object> setter;

		public PropertySetter(string fieldName, Action<System.Object> setter)
		{
			this.fieldName = fieldName;
			this.setter = setter;
		}

		public string GetFieldName()
		{
			return this.fieldName;
		}

		public void SetField(System.Object val)
		{
			if (this.setter != null)
			{
				this.setter(val);
			}
		}
	}
}