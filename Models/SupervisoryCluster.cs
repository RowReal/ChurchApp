using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    /// <summary>
    /// Represents a configurable supervisory grouping of Directorates.
    ///
    /// A cluster is independent of the person currently heading it.
    /// This allows the Cluster Head or the Directorates assigned to the
    /// cluster to be changed without changing application code.
    /// </summary>
    public class SupervisoryCluster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // Current worker appointed to oversee this cluster.
        // The person does not have to be an Assistant Pastor.
        public int? HeadWorkerId { get; set; }

        [ForeignKey(nameof(HeadWorkerId))]
        public Worker? HeadWorker { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastUpdated { get; set; }

        // Directorates currently assigned to this cluster.
        public virtual ICollection<SupervisoryClusterDirectorate>
            Directorates
        { get; set; } =
                new List<SupervisoryClusterDirectorate>();
    }


    /// <summary>
    /// Links a Directorate to a Supervisory Cluster.
    ///
    /// The separate mapping table allows Directorate assignments
    /// to be changed through configuration rather than application code.
    /// </summary>
    public class SupervisoryClusterDirectorate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SupervisoryClusterId { get; set; }

        [ForeignKey(nameof(SupervisoryClusterId))]
        public SupervisoryCluster? SupervisoryCluster { get; set; }

        [Required]
        public int DirectorateId { get; set; }

        [ForeignKey(nameof(DirectorateId))]
        public Directorate? Directorate { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}
