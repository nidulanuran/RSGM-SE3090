import 'dart:typed_data';

import '../../models/jobseeker/jobseeker_models.dart';
import '../api_client.dart';

class JobSeekerService {
  JobSeekerService({ApiClient? client}) : _client = client ?? ApiClient();

  final ApiClient _client;

  Future<JobSeekerDashboardStats> getDashboard() async =>
      JobSeekerDashboardStats.fromJson(
        Map<String, dynamic>.from(
          await _client.get('jobseeker/dashboard') as Map,
        ),
      );

  Future<List<JobSeekerJob>> getJobs() async =>
      (await _client.get('jobs') as List)
          .map(
            (e) => JobSeekerJob.fromJson(
              Map<String, dynamic>.from(e as Map),
            ),
          )
          .toList();

  Future<JobSeekerJob> getJob(String id) async =>
      JobSeekerJob.fromJson(
        Map<String, dynamic>.from(await _client.get('jobs/$id') as Map),
      );

  Future<List<JobSeekerApplication>> getApplications() async =>
      (await _client.get('jobseeker/applications') as List)
          .map(
            (e) => JobSeekerApplication.fromJson(
              Map<String, dynamic>.from(e as Map),
            ),
          )
          .toList();

  Future<JobSeekerApplication> apply(String jobId) async =>
      JobSeekerApplication.fromJson(
        Map<String, dynamic>.from(
          await _client.post(
            'jobseeker/applications',
            {'jobPostingId': jobId},
          ) as Map,
        ),
      );

  Future<void> withdraw(String applicationId) async =>
      _client.delete('jobseeker/applications/$applicationId');

  Future<JobSeekerProfile> getProfile() async =>
      JobSeekerProfile.fromJson(
        Map<String, dynamic>.from(
          await _client.get('jobseeker/profile') as Map,
        ),
      );

  Future<JobSeekerProfile> updateProfile(JobSeekerProfile profile) async =>
      JobSeekerProfile.fromJson(
        Map<String, dynamic>.from(
          await _client.put(
            'jobseeker/profile',
            profile.toUpdateJson(),
          ) as Map,
        ),
      );

  Future<List<JobSeekerSkill>> getSkills() async =>
      (await _client.get('jobseeker/skills') as List)
          .map(
            (e) => JobSeekerSkill.fromJson(
              Map<String, dynamic>.from(e as Map),
            ),
          )
          .toList();

  Future<List<SkillCatalogItem>> getSkillCatalog() async =>
      (await _client.get('skills') as List)
          .map(
            (e) => SkillCatalogItem.fromJson(
              Map<String, dynamic>.from(e as Map),
            ),
          )
          .where((e) => e.isActive)
          .toList();

  Future<JobSeekerSkill> addSkill(String skillId, int level) async =>
      JobSeekerSkill.fromJson(
        Map<String, dynamic>.from(
          await _client.post(
            'jobseeker/skills',
            {
              'skillId': skillId,
              'proficiencyLevel': level,
            },
          ) as Map,
        ),
      );

  Future<JobSeekerSkill> updateSkill(String skillId, int level) async =>
      JobSeekerSkill.fromJson(
        Map<String, dynamic>.from(
          await _client.put(
            'jobseeker/skills/$skillId/proficiency',
            {'proficiencyLevel': level},
          ) as Map,
        ),
      );

  Future<void> deleteSkill(String skillId) async =>
      _client.delete('jobseeker/skills/$skillId');

  Future<JobSeekerCv?> getCv() async {
    try {
      return JobSeekerCv.fromJson(
        Map<String, dynamic>.from(
          await _client.get('jobseeker/cv') as Map,
        ),
      );
    } on ApiException catch (e) {
      if (e.isNotFound) {
        return null;
      }

      rethrow;
    }
  }

  /// Uploads or replaces the current Job Seeker CV.
  ///
  /// The backend stores the file in Supabase and returns the new CV metadata.
  Future<JobSeekerCv> uploadCv({
    required String fileName,
    required Uint8List bytes,
    required String contentType,
  }) async {
    final response = await _client.postMultipartBytes(
      'jobseeker/cv',
      fieldName: 'file',
      fileName: fileName,
      bytes: bytes,
      contentType: contentType,
    );

    return JobSeekerCv.fromJson(
      Map<String, dynamic>.from(response as Map),
    );
  }

  Future<void> deleteCv() async {
    await _client.delete('jobseeker/cv');
  }
}
