class JobSeekerDashboardStats {
  JobSeekerDashboardStats({required this.fullName, required this.totalApplications, required this.activeApplications, required this.shortlistedApplications, required this.interviewApplications, required this.offerApplications, required this.availableJobs, required this.averageMatchScore, required this.skillsCount, required this.cvUploaded, required this.profileCompleteness, required this.topGapSkills});
  final String fullName;
  final int totalApplications, activeApplications, shortlistedApplications, interviewApplications, offerApplications, availableJobs, skillsCount, profileCompleteness;
  final double averageMatchScore;
  final bool cvUploaded;
  final List<String> topGapSkills;
  factory JobSeekerDashboardStats.fromJson(Map<String,dynamic> j) => JobSeekerDashboardStats(
    fullName: j['fullName']?.toString() ?? '', totalApplications: _i(j['totalApplications']), activeApplications: _i(j['activeApplications']), shortlistedApplications: _i(j['shortlistedApplications']), interviewApplications: _i(j['interviewApplications']), offerApplications: _i(j['offerApplications']), availableJobs: _i(j['availableJobs']), averageMatchScore: _d(j['averageMatchScore']), skillsCount: _i(j['skillsCount']), cvUploaded: j['cvUploaded'] == true, profileCompleteness: _i(j['profileCompleteness']), topGapSkills: _strings(j['topGapSkills']));
}

class JobSeekerJob {
  JobSeekerJob({required this.id, required this.title, required this.company, required this.location, required this.employmentType, required this.workMode, required this.experienceLevel, required this.description, required this.responsibilities, required this.requirements, required this.requiredSkills, this.minSalary, this.maxSalary, this.currency, this.applicationDeadline});
  final String id,title,company,location,employmentType,workMode,experienceLevel,description,responsibilities,requirements;
  final List<String> requiredSkills;
  final double? minSalary,maxSalary;
  final String? currency,applicationDeadline;
  factory JobSeekerJob.fromJson(Map<String,dynamic> j) => JobSeekerJob(id:j['id']?.toString()??'', title:j['title']?.toString()??'', company:j['company']?.toString()??'', location:j['location']?.toString()??'', employmentType:j['employmentType']?.toString()??'', workMode:j['workMode']?.toString()??'', experienceLevel:j['experienceLevel']?.toString()??'', description:j['description']?.toString()??'', responsibilities:j['responsibilities']?.toString()??'', requirements:j['requirements']?.toString()??'', requiredSkills:_strings(j['requiredSkills']), minSalary:_nullableDouble(j['minSalary']), maxSalary:_nullableDouble(j['maxSalary']), currency:j['currency']?.toString(), applicationDeadline:j['applicationDeadline']?.toString());
  String get salaryLabel {
    if (minSalary == null && maxSalary == null) return 'Salary not specified';
    final c = (currency == null || currency!.isEmpty) ? '' : '$currency ';
    if (minSalary != null && maxSalary != null) return '$c${minSalary!.toStringAsFixed(0)} - ${maxSalary!.toStringAsFixed(0)}';
    return '$c${(minSalary ?? maxSalary)!.toStringAsFixed(0)}';
  }
}

class JobSeekerApplication {
  JobSeekerApplication({required this.id, required this.jobPostingId, required this.jobTitle, required this.company, required this.status, required this.appliedAt, required this.matchScore, required this.matchedSkills, required this.gapSkills});
  final String id,jobPostingId,jobTitle,company,status,appliedAt;
  final int matchScore;
  final List<String> matchedSkills,gapSkills;
  factory JobSeekerApplication.fromJson(Map<String,dynamic> j)=>JobSeekerApplication(id:j['id']?.toString()??'', jobPostingId:j['jobPostingId']?.toString()??'', jobTitle:j['jobTitle']?.toString()??'', company:j['company']?.toString()??'', status:j['status']?.toString()??'', appliedAt:j['appliedAt']?.toString()??'', matchScore:_i(j['matchScore']), matchedSkills:_strings(j['matchedSkills']), gapSkills:_strings(j['gapSkills']));
}

class JobSeekerProfile {
  JobSeekerProfile({required this.fullName, required this.email, required this.headline, required this.location, required this.bio, required this.linkedInUrl, required this.gitHubUrl, required this.portfolioUrl});
  final String fullName,email,headline,location,bio,linkedInUrl,gitHubUrl,portfolioUrl;
  factory JobSeekerProfile.fromJson(Map<String,dynamic> j)=>JobSeekerProfile(fullName:j['fullName']?.toString()??'',email:j['email']?.toString()??'',headline:j['headline']?.toString()??'',location:j['location']?.toString()??'',bio:j['bio']?.toString()??'',linkedInUrl:j['linkedInUrl']?.toString()??'',gitHubUrl:j['gitHubUrl']?.toString()??'',portfolioUrl:j['portfolioUrl']?.toString()??'');
  Map<String,dynamic> toUpdateJson()=>{'fullName':fullName,'headline':headline,'location':location,'bio':bio,'linkedInUrl':linkedInUrl.isEmpty?null:linkedInUrl,'gitHubUrl':gitHubUrl.isEmpty?null:gitHubUrl,'portfolioUrl':portfolioUrl.isEmpty?null:portfolioUrl};
}

class JobSeekerSkill {
  JobSeekerSkill({required this.skillId,required this.name,required this.proficiencyLevel});
  final String skillId,name; final int proficiencyLevel;
  factory JobSeekerSkill.fromJson(Map<String,dynamic> j)=>JobSeekerSkill(skillId:j['skillId']?.toString()??'',name:j['name']?.toString()??'',proficiencyLevel:_i(j['proficiencyLevel']));
}

class SkillCatalogItem {
  SkillCatalogItem({required this.id,required this.name,required this.isActive});
  final String id,name; final bool isActive;
  factory SkillCatalogItem.fromJson(Map<String,dynamic> j)=>SkillCatalogItem(id:j['id']?.toString()??'',name:j['name']?.toString()??'',isActive:j['isActive']!=false);
}

class JobSeekerCv {
  JobSeekerCv({required this.fileName,required this.fileSizeBytes,required this.uploadedAt});
  final String fileName,uploadedAt; final int fileSizeBytes;
  factory JobSeekerCv.fromJson(Map<String,dynamic> j)=>JobSeekerCv(fileName:j['fileName']?.toString()??'',fileSizeBytes:_i(j['fileSizeBytes']),uploadedAt:j['uploadedAt']?.toString()??'');
}

int _i(dynamic v)=>v is num?v.toInt():int.tryParse(v?.toString()??'')??0;
double _d(dynamic v)=>v is num?v.toDouble():double.tryParse(v?.toString()??'')??0;
double? _nullableDouble(dynamic v)=>v==null?null:_d(v);
List<String> _strings(dynamic v)=>v is List?v.map((e)=>e.toString()).toList():<String>[];
