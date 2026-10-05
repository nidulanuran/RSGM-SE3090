import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../services/jobseeker/jobseeker_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/rsgm_widgets.dart';
import '../home_page.dart';
import 'jobseeker_applications_screen.dart';
import 'jobseeker_dashboard_screen.dart';
import 'jobseeker_jobs_screen.dart';
import 'jobseeker_profile_screen.dart';

class JobSeekerMainScreen extends StatefulWidget {
  const JobSeekerMainScreen({super.key, this.initialIndex=0, this.service});
  final int initialIndex;
  final JobSeekerService? service;
  @override State<JobSeekerMainScreen> createState()=>_JobSeekerMainScreenState();
}

class _JobSeekerMainScreenState extends State<JobSeekerMainScreen> {
  late int _index;
  late final JobSeekerService _service;
  @override void initState(){ super.initState(); _index=widget.initialIndex; _service=widget.service??JobSeekerService(); }

  Future<void> _logout() async {
    final ok=await showDialog<bool>(context:context,builder:(c)=>AlertDialog(title:const Text('Sign out'),content:const Text('Are you sure you want to sign out of your Job Seeker account?'),actions:[TextButton(onPressed:()=>Navigator.pop(c,false),child:const Text('Cancel')),FilledButton(onPressed:()=>Navigator.pop(c,true),child:const Text('Sign out'))]));
    if(ok!=true||!mounted)return;
    const secure=FlutterSecureStorage(); await secure.delete(key:'rsgm_token');
    final prefs=await SharedPreferences.getInstance(); await prefs.remove('rsgm_session_token'); await prefs.remove('rsgm_roles'); await prefs.remove('rsgm_user');
    if(!mounted)return; Navigator.pushAndRemoveUntil(context,MaterialPageRoute(builder:(_)=>const HomePage()),(_)=>false);
  }

  @override Widget build(BuildContext context)=>Scaffold(
    appBar:AppBar(backgroundColor:Colors.white,elevation:0,scrolledUnderElevation:1,title:Row(children:[const RsgmBrand(compact:true),const SizedBox(width:10),Container(padding:const EdgeInsets.symmetric(horizontal:8,vertical:4),decoration:BoxDecoration(color:const Color(0xFFECFDF5),borderRadius:BorderRadius.circular(12),border:Border.all(color:const Color(0xFFA7F3D0))),child:const Text('Job Seeker',style:TextStyle(fontSize:12,fontWeight:FontWeight.w700,color:Color(0xFF047857))))]),actions:[IconButton(tooltip:'Sign out',onPressed:_logout,icon:const Icon(Icons.logout_rounded,color:AppTheme.muted))]),
    body:IndexedStack(index:_index,children:[JobSeekerDashboardScreen(service:_service,onBrowseJobs:()=>setState(()=>_index=1)),JobSeekerJobsScreen(service:_service),JobSeekerApplicationsScreen(service:_service),JobSeekerProfileScreen(service:_service)]),
    bottomNavigationBar:BottomNavigationBar(currentIndex:_index,onTap:(i)=>setState(()=>_index=i),type:BottomNavigationBarType.fixed,selectedItemColor:AppTheme.violet,unselectedItemColor:AppTheme.muted,items:const [BottomNavigationBarItem(icon:Icon(Icons.dashboard_outlined),activeIcon:Icon(Icons.dashboard_rounded),label:'Dashboard'),BottomNavigationBarItem(icon:Icon(Icons.search_rounded),activeIcon:Icon(Icons.work_rounded),label:'Jobs'),BottomNavigationBarItem(icon:Icon(Icons.assignment_outlined),activeIcon:Icon(Icons.assignment_rounded),label:'Applications'),BottomNavigationBarItem(icon:Icon(Icons.person_outline_rounded),activeIcon:Icon(Icons.person_rounded),label:'Profile')]),
  );
}
