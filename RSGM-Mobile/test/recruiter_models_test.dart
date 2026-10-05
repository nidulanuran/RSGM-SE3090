import 'package:flutter_test/flutter_test.dart';
import 'package:rsgm_mobile/models/recruiter/recruiter_busy_time.dart';
import 'package:rsgm_mobile/models/recruiter/recruiter_job_post.dart';
import 'package:rsgm_mobile/models/recruiter/recruiter_applicant.dart';
import 'package:rsgm_mobile/models/recruiter/recruiter_requisition.dart';

void main() {
  group('RecruiterBusyTime model tests', () {
    test('fromJson and toJson roundtrip with full payload', () {
      final json = {
        'id': 'bt-123',
        'title': 'Dentist Appointment',
        'description': 'Out of office',
        'startsAt': '2026-10-10T09:00:00.000Z',
        'endsAt': '2026-10-10T11:00:00.000Z',
        'cancelledInterviews': 2,
      };

      final model = RecruiterBusyTime.fromJson(json);

      expect(model.id, 'bt-123');
      expect(model.title, 'Dentist Appointment');
      expect(model.description, 'Out of office');
      expect(model.cancelledInterviews, 2);
      expect(model.duration.inHours, 2);

      final outJson = model.toJson();
      expect(outJson['title'], 'Dentist Appointment');
      expect(outJson['description'], 'Out of office');
      expect(outJson['startsAt'], '2026-10-10T09:00:00.000Z');
      expect(outJson['endsAt'], '2026-10-10T11:00:00.000Z');
    });

    test('fromJson handles minimal payload and null description', () {
      final json = {
        'id': 'bt-456',
        'title': 'Internal Meeting',
        'startsAt': '2026-10-10T14:00:00.000Z',
        'endsAt': '2026-10-10T15:00:00.000Z',
      };

      final model = RecruiterBusyTime.fromJson(json);

      expect(model.id, 'bt-456');
      expect(model.title, 'Internal Meeting');
      expect(model.description, isNull);
      expect(model.cancelledInterviews, isNull);

      final outJson = model.toJson();
      expect(outJson.containsKey('description'), isFalse);
    });
  });

  group('RecruiterJobPost model tests', () {
    test('fromJson handles standard string enum response', () {
      final json = {
        'id': 'job-001',
        'jobRequisitionId': 'req-001',
        'title': 'Senior Flutter Engineer',
        'company': 'Tech Corp',
        'companyLogoUrl': 'https://example.com/logo.png',
        'location': 'Colombo, Sri Lanka',
        'description': 'Build cross-platform applications',
        'employmentType': 'FullTime',
        'workMode': 'Hybrid',
        'responsibilities': 'Develop features, write tests',
        'requirements': 'Dart, Flutter, CI/CD',
        'experienceLevel': 'Senior',
        'minExperienceYears': 5,
        'minSalary': 350000.0,
        'maxSalary': 500000.0,
        'currency': 'LKR',
        'applicationDeadline': '2026-12-31',
        'status': 'Published',
        'applicantCount': 12,
        'createdAt': '2026-10-01T08:00:00.000Z',
        'requiredSkills': [
          {'id': 'sk-1', 'name': 'Flutter', 'weight': 1.5},
          {'id': 'sk-2', 'name': 'Dart', 'weight': 1.2},
        ],
      };

      final job = RecruiterJobPost.fromJson(json);

      expect(job.id, 'job-001');
      expect(job.isPublished, isTrue);
      expect(job.isDraft, isFalse);
      expect(job.isClosed, isFalse);
      expect(job.employmentTypeLabel, 'Full-Time');
      expect(job.workModeLabel, 'Hybrid');
      expect(job.experienceLevelLabel, 'Senior');
      expect(job.applicantCount, 12);
      expect(job.requiredSkills.length, 2);
      expect(job.requiredSkills.first.name, 'Flutter');
      expect(job.salaryRangeFormatted, 'LKR 350,000 – 500,000');
    });

    test('fromJson handles numeric strings in enum fields', () {
      final json = {
        'id': 'job-002',
        'title': 'Junior QA Engineer',
        'company': 'QA Labs',
        'location': 'Remote',
        'employmentType': '1', // PartTime
        'workMode': '1', // Remote
        'responsibilities': 'Manual and automated testing',
        'requirements': 'Selenium, Jest',
        'experienceLevel': '1', // Junior
        'status': '1', // Published
        'applicantCount': 3,
        'createdAt': '2026-10-02T10:00:00.000Z',
      };

      final job = RecruiterJobPost.fromJson(json);

      expect(job.isPublished, isTrue);
      expect(job.employmentTypeLabel, 'Part-Time');
      expect(job.workModeLabel, 'Remote');
      expect(job.experienceLevelLabel, 'Junior');
      expect(job.salaryRangeFormatted, 'Salary not disclosed');
    });
  });

  group('RecruiterApplicant model tests', () {
    test('fromJson parses full applicant with skills and match breakdown', () {
      final json = {
        'id': 'app-001',
        'jobPostingId': 'job-001',
        'jobTitle': 'Senior Flutter Engineer',
        'candidateId': 'cand-001',
        'fullName': 'Kasun Perera',
        'email': 'kasun@example.com',
        'phoneNumber': '+94771234567',
        'headline': 'Lead Mobile Architect',
        'location': 'Colombo',
        'bio': 'Passionate about Clean Architecture',
        'linkedInUrl': 'https://linkedin.com/in/kasun',
        'gitHubUrl': 'https://github.com/kasun',
        'portfolioUrl': 'https://kasun.dev',
        'hasCv': true,
        'cvFileName': 'kasun_cv.pdf',
        'status': 'Shortlisted',
        'shortlistRank': 1,
        'appliedAt': '2026-10-02T12:00:00.000Z',
        'matchScore': 92,
        'exactMatchScore': 91.5,
        'matchExplanation': 'Strong match on Flutter and Dart',
        'matchBreakdown': [
          {
            'skillId': 'sk-1',
            'skillName': 'Flutter',
            'requiredWeight': 1.5,
            'candidateProficiency': 5,
            'proficiencyLabel': 'Expert',
            'contributionPercentage': 60.0,
            'matched': true,
          }
        ],
        'skills': ['Flutter', 'Dart', 'Git'],
        'matchedSkills': ['Flutter', 'Dart'],
        'missingSkills': ['CI/CD'],
        'education': ['BSc in Software Engineering - SLIIT'],
        'workExperience': ['Senior Developer at ABC (3 years)'],
      };

      final applicant = RecruiterApplicant.fromJson(json);

      expect(applicant.id, 'app-001');
      expect(applicant.fullName, 'Kasun Perera');
      expect(applicant.initials, 'KP');
      expect(applicant.isShortlisted, isTrue);
      expect(applicant.isUnderReview, isFalse);
      expect(applicant.statusLabel, 'Shortlisted');
      expect(applicant.shortlistRank, 1);
      expect(applicant.hasCv, isTrue);
      expect(applicant.cvFileName, 'kasun_cv.pdf');
      expect(applicant.matchScore, 92);
      expect(applicant.exactMatchScore, 91.5);
      expect(applicant.matchBreakdown.length, 1);
      expect(applicant.matchBreakdown.first.proficiencyLabel, 'Expert');
      expect(applicant.skills, contains('Flutter'));
      expect(applicant.matchedSkills.length, 2);
      expect(applicant.missingSkills.length, 1);
    });

    test('initials computation handles edge cases', () {
      final a1 = RecruiterApplicant.fromJson({
        'id': '1',
        'fullName': 'Kasun',
      });
      expect(a1.initials, 'K');

      final a2 = RecruiterApplicant.fromJson({
        'id': '2',
        'fullName': '',
      });
      expect(a2.initials, '?');
    });

    test('statusLabel and helper getters handle numeric and string statuses', () {
      final app0 = RecruiterApplicant.fromJson({'id': '1', 'status': '0'});
      expect(app0.isUnderReview, isTrue);
      expect(app0.statusLabel, 'Under Review');

      final app1 = RecruiterApplicant.fromJson({'id': '2', 'status': '1'});
      expect(app1.isShortlisted, isTrue);
      expect(app1.statusLabel, 'Shortlisted');

      final app2 = RecruiterApplicant.fromJson({'id': '3', 'status': '2'});
      expect(app2.isInterview, isTrue);
      expect(app2.statusLabel, 'Interview');

      final app4 = RecruiterApplicant.fromJson({'id': '4', 'status': '4'});
      expect(app4.isRejected, isTrue);
      expect(app4.statusLabel, 'Rejected');
    });
  });

  group('RecruiterRequisition model tests', () {
    test('fromJson parses requisition with numeric enums (ASP.NET default)', () {
      final json = {
        'id': 'req-001',
        'companyId': 'comp-001',
        'companyName': 'Acme Inc',
        'recruiterId': 'rec-001',
        'recruiterName': 'John Recruiter',
        'positionTitle': 'Backend Engineer',
        'department': 'Engineering',
        'headcount': 2,
        'employmentType': 0, // FullTime
        'workMode': 2, // Hybrid
        'location': 'Colombo',
        'experienceLevel': 3, // Senior
        'minExperienceYears': 4,
        'minSalary': 250000.0,
        'maxSalary': 400000.0,
        'currency': 'LKR',
        'description': 'Develop scalable APIs',
        'responsibilities': 'API design, mentoring',
        'requirements': '.NET 8, PostgreSQL',
        'justification': 'Expansion of fintech squad',
        'status': 3, // Approved
        'hrFeedback': 'Approved by HR Lead',
        'reviewedByUserId': 'hr-001',
        'reviewedByName': 'Jane HR',
        'reviewedAt': '2026-10-02T15:00:00.000Z',
        'createdAt': '2026-10-01T09:00:00.000Z',
        'approvedAt': '2026-10-02T15:00:00.000Z',
      };

      final req = RecruiterRequisition.fromJson(json);

      expect(req.id, 'req-001');
      expect(req.positionTitle, 'Backend Engineer');
      expect(req.department, 'Engineering');
      expect(req.headcount, 2);
      expect(req.employmentType, RequisitionEmploymentType.fullTime);
      expect(req.employmentType.label, 'Full-Time');
      expect(req.workMode, RequisitionWorkMode.hybrid);
      expect(req.workMode.label, 'Hybrid');
      expect(req.experienceLevel, RequisitionExperienceLevel.senior);
      expect(req.experienceLevel.label, 'Senior');
      expect(req.status, RequisitionStatus.approved);
      expect(req.status.label, 'Approved');
      expect(req.isApproved, isTrue);
      expect(req.isDraft, isFalse);
      expect(req.isRejected, isFalse);
      expect(req.isSubmitted, isFalse);
      expect(req.hrFeedback, 'Approved by HR Lead');
      expect(req.reviewedByName, 'Jane HR');
      expect(req.salaryRangeFormatted, 'LKR 250,000 – 400,000');
    });

    test('fromJson parses requisition with string enums', () {
      final json = {
        'id': 'req-002',
        'companyId': 'comp-001',
        'companyName': 'Acme Inc',
        'recruiterId': 'rec-001',
        'recruiterName': 'John Recruiter',
        'positionTitle': 'Product Designer',
        'department': 'Design',
        'headcount': 1,
        'employmentType': 'Contract',
        'workMode': 'Remote',
        'location': 'Remote',
        'experienceLevel': 'Mid',
        'status': 'Submitted',
        'currency': 'USD',
        'minSalary': 2000.0,
        'maxSalary': 3000.0,
        'createdAt': '2026-10-03T09:00:00.000Z',
      };

      final req = RecruiterRequisition.fromJson(json);

      expect(req.employmentType, RequisitionEmploymentType.contract);
      expect(req.employmentType.label, 'Contract');
      expect(req.workMode, RequisitionWorkMode.remote);
      expect(req.workMode.label, 'Remote');
      expect(req.experienceLevel, RequisitionExperienceLevel.mid);
      expect(req.experienceLevel.label, 'Mid Level');
      expect(req.status, RequisitionStatus.submitted);
      expect(req.isSubmitted, isTrue);
      expect(req.salaryRangeFormatted, 'USD 2,000 – 3,000');
    });

    test('enum parsing fallback handles invalid or null values gracefully', () {
      expect(RequisitionStatus.fromJson(null), RequisitionStatus.draft);
      expect(RequisitionStatus.fromJson('Unknown'), RequisitionStatus.draft);
      expect(RequisitionEmploymentType.fromJson(null), RequisitionEmploymentType.fullTime);
      expect(RequisitionWorkMode.fromJson(null), RequisitionWorkMode.onSite);
      expect(RequisitionExperienceLevel.fromJson(null), RequisitionExperienceLevel.entry);
    });
  });
}
