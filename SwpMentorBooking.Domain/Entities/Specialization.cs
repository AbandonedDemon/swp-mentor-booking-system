﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SwpMentorBooking.Domain.Entities;

public partial class Specialization
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<MentorDetail> MentorDetails { get; set; } = new List<MentorDetail>();
}
