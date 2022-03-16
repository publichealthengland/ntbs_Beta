﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EFAuditer
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string OriginalId { get; set; }
        public string EntityType { get; set; }
        public string EventType { get; set; }
        public string AuditDetails { get; set; }
        public string AuditData { get; set; }
        public DateTime AuditDateTime { get; set; }
        public string AuditUser { get; set; }
        public string RootEntity { get; set; }
        public string RootId { get; set; }

        [NotMapped]
        public string ActionString { get; set; }
        [NotMapped]
        public string SubjectString { get; set; }

        public AuditLog Clone()
        {
            return MemberwiseClone() as AuditLog;
        }

        public override string ToString()
        {
            return
                $@"Log({Id}, {OriginalId}, {EntityType}, {EventType}, {AuditDetails}, {AuditData}, {AuditDateTime}, {AuditUser}, {RootEntity}, {RootId})";
        }
    }
}
