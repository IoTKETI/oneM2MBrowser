using System.Collections.Generic;

namespace MobiusResourceMonitor_sub
{
    public class ResourceObject
    {
        public string ResourcePath
        {
            get 
            { 
                return this.resource_path;
            }
            set 
            {
                this.resource_path = value;
                this.path_level = GetPathLevel();
            } 
        }
        public string ResourceName { get; set; }
        public string ParentPath { get; set; }
        public string ResourceType { get; set; }
        public ResourceStatusOption ResourceStatus
        {
            get
            {
                return this.resource_status;
            }
            set
            {
                this.resource_status = value;
            }
        }
        public int Level
        {
            get { return this.path_level; }
        }

        private int path_level = 0;
        private string resource_path  = "";
        private ResourceStatusOption resource_status = ResourceStatusOption.Normal;

        private int GetPathLevel()
        {
            int result = 0;

            if (resource_path.Length > 0)
            {
                result = resource_path.Split('/').Length - 1;
            }

            return result;
        }
    }
    public enum ResourceStatusOption
    {
        Normal, New, Old
    }
}
