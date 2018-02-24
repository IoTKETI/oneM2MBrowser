/**
 * Copyright (c) 2015, OCEAN
 * All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/**
 * Created by Chen Nan in KETI on 2016-07-28.
 */
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
        public string AccessControlPolicy { get; set; }
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
